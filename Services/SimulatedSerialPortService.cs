using System;
using System.Collections.Generic;
using System.Timers;
using Timer = System.Timers.Timer;

namespace OscilloscopeApp.Services
{
    public class SimulatedSerialPortService : ISerialPortService
    {
        public bool IsOpen { get; private set; } = false;
        public IEnumerable<string> GetPortNames() => new[] { "COM_SIMULATE" };
        public void Open(string portName, int baudRate) => IsOpen = true;
        public void Close() => IsOpen = false;

        // Sự kiện mô phỏng dữ liệu đến
        public event EventHandler<byte[]>? SimulatedDataReceived;

        private Timer _timer;

        public SimulatedSerialPortService()
        {
            _timer = new Timer(1); // 100Hz
            _timer.Elapsed += (s, e) => RaiseSimulatedData();
        }

        public void StartSimulate()
        {
            _timer.Start();
        }

        public void StopSimulate()
        {
            _timer.Stop();
        }

        private void RaiseSimulatedData()
        {
            if (SimulatedDataReceived != null)
            {
                var packet = BuildSinFramePacket(0x55, 0x01, 0xAA);
                // print packet in hex for debug
                // Console.WriteLine($"Org Raw: {string.Join(" ", packet.Select(b => b.ToString("X2")))}");
                var encoded = EncodePacket(packet, new UartOptions { Header = 0x55, Eop = 0xAA, Esc = 0x7D });
                SimulatedDataReceived(this, encoded);
            }
        }
        public short[] counter = new short[8];
        private int total = 0;
        private const int channelCount = 8;

        public short[][] GenerateSinFrame()
        {
            short[][] frame = new short[10][];
            for (int group = 0; group < 10; group++)
            {
                frame[group] = new short[channelCount];
                for (int i = 0; i < channelCount; i++)
                {
                    double phase = i * Math.PI / 9; // phase khác nhau cho từng kênh
                    frame[group][i] = (short)(Math.Sin((total + group) * 0.01 + phase) * 30000);

                    // frame[group][i] = counter[i];
                    // // if (total > 30000) frame[group][i] = -30000;
                    // counter[i] += (short)i;
                    // if (counter[i] > 30000) counter[i] = -30000;

                }
            }
            total += 10;
            // print raw frame in number for debug
            // Console.WriteLine($"Org frame: {string.Join(", ", frame.SelectMany(g => g).Select(v => v.ToString()))}");
            return frame;
        }

        public byte[] BuildSinFramePacket(byte header, byte address, byte eop)
        {
            var frame = GenerateSinFrame();
            var packet = new List<byte>();

            packet.Add(header);
            packet.Add(address);

            foreach (var group in frame)
            {
                foreach (var value in group)
                {
                    packet.Add((byte)(value >> 8));    // High byte
                    packet.Add((byte)(value & 0xFF));  // Low byte
                }
            }

            ushort crc = CalculateCRC16(packet.ToArray(), 1, packet.Count - 1); // Tính CRC từ Address đến cuối dữ liệu
            packet.Add((byte)(crc >> 8));    // CRC high byte
            packet.Add((byte)(crc & 0xFF));  // CRC low byte

            packet.Add(eop);

            return packet.ToArray();
        }

        // CRC-16-CCITT (poly 0x1021, initial 0xFFFF)
        private static ushort CalculateCRC16(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = offset; i < offset + length; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }

        public static byte[] EncodePacket(byte[] data, UartOptions options)
        {
            var encoded = new List<byte>();
            encoded.Add(options.Header);

            for (int i = 1; i < data.Length - 1; i++)
            {
                byte b = data[i];
                if (b == options.Eop || b == options.Esc || b == options.Header)
                {
                    encoded.Add(options.Esc);
                    encoded.Add((byte)(b ^ 0x20));
                }
                else
                {
                    encoded.Add(b);
                }
            }

            encoded.Add(options.Eop);
            return encoded.ToArray();
        }
    }
}