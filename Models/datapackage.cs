using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.IO.Pipelines;

namespace UartReceiver
{
    public sealed class UartOptions
    {
        public byte Header { get; set; } = 0x55; // Changed from init-only to settable property
        public byte Address { get; set; } = 0x01; // Changed from init-only to settable property
        public byte Eop { get; set; } = 0xAA; // Changed from init-only to settable property
        public byte Esc { get; set; } = 0x7D; // Changed from init-only to settable property
        public int ExpectedDecodedLength { get; set; } = 165; // Changed from init-only to settable property
        public int CrcOffset { get; set; } = 162; // Changed from init-only to settable property
        public int CrcCount { get; set; } = 161; // Changed from init-only to settable property
        public bool CrcLittleEndian { get; set; } = false; // Changed from init-only to settable property
        public int MaxDecodedPacketSize { get; set; } = 256; // Changed from init-only to settable property
    }

    // Replace `required` keyword with constructor-based initialization to make the code compatible with C# 8.0.

    public sealed class Packet
    {
        public byte[] Raw { get; set; }          // Gói đã unescape (bao gồm HEADER...EOP)
        public bool CrcOk { get; set; }          // CRC hợp lệ
        public long Sequence { get; set; }       // Số thứ tự gói (giữ thứ tự hiển thị)

        // Constructor to ensure required properties are initialized
        public Packet(byte[] raw, bool crcOk, long sequence)
        {
            Raw = raw ?? throw new ArgumentNullException(nameof(raw));
            CrcOk = crcOk;
            Sequence = sequence;
        }
    }

    // CRC-16 CIIT: poly 0x1021, init 0xFFFF, không đảo bit, không xor out
    public static class Crc16Ciit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Compute(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in data)
            {
                crc ^= (ushort)(b << 8);
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }
    }

    // State machine parser: ăn Span<byte> liên tục, unescape bằng XOR 0x20, yield gói khi gặp EOP thật
    public sealed class PacketFramer
    {
        private readonly UartOptions _opt;
        private bool _receiving;
        private bool _escPending;
        private List<byte> _builder;

        public PacketFramer(UartOptions? opt = null)
        {
            _opt = opt ?? new UartOptions();
            _builder = new List<byte>(_opt.ExpectedDecodedLength);
        }

        // Feed dữ liệu mới, trả về danh sách gói đã unescape (mỗi gói bao gồm HEADER...EOP)
        public void Feed(ReadOnlySpan<byte> buffer, List<byte[]> output)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                byte b = buffer[i];

                if (!_receiving)
                {
                    if (b == _opt.Header)
                    {
                        _receiving = true;
                        _escPending = false;
                        _builder = new List<byte>(_opt.ExpectedDecodedLength);
                        _builder.Add(b);
                    }
                    continue;
                }

                if (_escPending)
                {
                    // byte sau ESC đã được XOR 0x20 ở phía gửi -> khôi phục bằng XOR 0x20
                    byte unescaped = (byte)(b ^ 0x20);
                    AppendByte(unescaped, output);
                    _escPending = false;
                    continue;
                }

                if (b == _opt.Esc)
                {
                    _escPending = true;
                    continue;
                }

                if (b == _opt.Eop)
                {
                    // Kết gói: thêm EOP "thật" (không unescape)
                    AppendRawEop(output);
                    _receiving = false;
                    _escPending = false;
                    continue;
                }

                AppendByte(b, output);
            }
        }

        private void AppendByte(byte value, List<byte[]> output)
        {
            if (_builder.Count >= _opt.MaxDecodedPacketSize)
            {
                ResetFrame();
                return;
            }
            _builder.Add(value);
        }

        private void AppendRawEop(List<byte[]> output)
        {
            if (_builder.Count + 1 > _opt.MaxDecodedPacketSize)
            {
                ResetFrame();
                return;
            }

            _builder.Add(_opt.Eop);

            var arr = _builder.ToArray();

            if (arr.Length >= 4 && arr[0] == _opt.Header)
            {
                output.Add(arr);
            }

            ResetFrame();
        }

        private void ResetFrame()
        {
            _receiving = false;
            _escPending = false;
            _builder = new List<byte>(_opt.ExpectedDecodedLength);
        }
    }

    // Service tích hợp SerialPort, Channel, CRC song song và event về UI
    public sealed class UartReceiverService : IDisposable
    {
        private readonly SerialPort _port;
        private readonly UartOptions _opt;
        private readonly PacketFramer _framer;
        private readonly Channel<byte[]> _packetChannel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Control? _uiControl; // WinForms control để Invoke về UI
        private long _sequence;

        public event EventHandler<Packet>? PacketReceived;

        public UartReceiverService(string portName, int baudRate, Control? uiControl, UartOptions? options = null)
        {
            _opt = options ?? new UartOptions();
            _framer = new PacketFramer(_opt);
            _packetChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            });

            _uiControl = uiControl;

            _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = -1,
                WriteTimeout = -1
            };
            _port.ReadBufferSize = 16384; // Tăng kích thước buffer đọc
            _port.DataReceived += OnDataReceived;
        }

        public void Start()
        {
            if (!_port.IsOpen) _port.Open();

            // Spin up workers bằng số lõi CPU
            int workers = Math.Max(2, Environment.ProcessorCount);
            for (int i = 0; i < workers; i++)
            {
                _ = Task.Run(() => WorkerAsync(_cts.Token));
            }
        }

        public void Stop()
        {
            _cts.Cancel();
            if (_port.IsOpen) _port.Close();
        }

        private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int n = _port.BytesToRead;
                if (n <= 0) return;

                // Đọc chunk
                byte[] temp = ArrayPool<byte>.Shared.Rent(n);
                int read = _port.Read(temp, 0, n);

                var span = new ReadOnlySpan<byte>(temp, 0, read);

                // Parse ngay theo dạng streaming (không cần giữ buffer toàn cục)
                var packets = new List<byte[]>(capacity: 8);
                _framer.Feed(span, packets);

                // Đẩy vào channel
                foreach (var p in packets)
                {
                    _packetChannel.Writer.TryWrite(p);
                }

                ArrayPool<byte>.Shared.Return(temp);
            }
            catch
            {
                // Có thể log nếu cần
            }
        }

        private async Task WorkerAsync(CancellationToken ct)
        {
            // Worker: đọc gói đã unescape, verify CRC, raise event về UI
            while (await _packetChannel.Reader.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                while (_packetChannel.Reader.TryRead(out var raw))
                {
                    bool crcOk = VerifyCrc(raw);
                    long seq = Interlocked.Increment(ref _sequence);

                    var pkt = new Packet(raw, crcOk, seq);

                    // Invoke về UI thread an toàn
                    if (_uiControl != null && _uiControl.IsHandleCreated)
                    {
                        try
                        {
                            _uiControl.BeginInvoke(new Action(() =>
                            {
                                PacketReceived?.Invoke(this, pkt);
                            }));
                        }
                        catch
                        {
                            // ignore nếu form đóng
                        }
                    }
                    else
                    {
                        // Không có UI control -> raise trực tiếp
                        PacketReceived?.Invoke(this, pkt);
                    }
                }
            }
        }

        private bool VerifyCrc(byte[] decodedPacket)
        {
            if (decodedPacket.Length != _opt.ExpectedDecodedLength) return false;
            if (decodedPacket[0] != _opt.Header) return false;
            if (decodedPacket[decodedPacket.Length - 1] != _opt.Eop) return false;

            ushort received = _opt.CrcLittleEndian
                ? BinaryPrimitives.ReadUInt16LittleEndian(decodedPacket.AsSpan(_opt.CrcOffset, 2))
                : BinaryPrimitives.ReadUInt16BigEndian(decodedPacket.AsSpan(_opt.CrcOffset, 2));

            ushort computed = Crc16Ciit.Compute(decodedPacket.AsSpan(1, _opt.CrcCount));
            return received == computed;
        }

        public void Dispose()
        {
            Stop();
            _port.Dispose();
            _cts.Dispose();
        }
    }
}
