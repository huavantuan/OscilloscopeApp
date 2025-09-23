using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualBasic.Logging;
using OscilloscopeApp.Services;
using System.Buffers.Binary;

namespace OscilloscopeApp.ViewModels
{
    public partial class SimulateViewModel : ObservableObject
    {
        [ObservableProperty] private bool isSimulating = false;
        public string SimulateButtonText => IsSimulating ? "Stop Simulate" : "Start Simulate";
        public string SimulateButtonColor => IsSimulating ? "OrangeRed" : "LightGreen";

        private readonly SimulatedSerialPortService _simService;

        // Thêm event chuyển tiếp từ SimulatedSerialPortService
        public event EventHandler<short[][]>? SimulatedDataReceived;

        private long packetErrorCount = 0;
        private long totalPackets = 0;
        private System.Timers.Timer _timer = new System.Timers.Timer(10); // 100Hz;
        public SimulateViewModel(SimulatedSerialPortService simService)
        {
            _simService = simService ?? throw new ArgumentNullException(nameof(simService));
            _simService.SimulatedDataReceived += OnSimulatedDataReceived;
            _timer.Elapsed += (s, e) => OnTimerElapsed();
        }

        partial void OnIsSimulatingChanged(bool value)
        {
            if (value)
            {
                _simService.StartSimulate();
                // _timer.Start();
            }
            else
            {
                _simService.StopSimulate();
                // _timer.Stop();
            }
                

            OnPropertyChanged(nameof(SimulateButtonText));
            OnPropertyChanged(nameof(SimulateButtonColor));
        }

        [RelayCommand]
        private void SimulateButton()
        {
            IsSimulating = !IsSimulating;
            Console.WriteLine($"SimulateButton clicked. Now IsSimulating={IsSimulating}");
        }
        private void OnTimerElapsed()
        {
            int channelCount = 8;
            int samplesPerGroup = 10;
            short[][] sindata = new short[channelCount][];
            for (int ch = 0; ch < channelCount; ch++)
            {
                sindata[ch] = new short[samplesPerGroup];
                double phase = ch * Math.PI / 9;
                for (int i = 0; i < samplesPerGroup; i++)
                {
                    sindata[ch][i] = (short)(Math.Sin((totalPackets + i) * 0.01 + phase) * 30000);
                }
            }
            SimulatedDataReceived?.Invoke(this, sindata);
            totalPackets += samplesPerGroup;
        }
        // Giống như xử lý trong SerialViewModel: decode frame trước khi chuyển tiếp
        private void OnSimulatedDataReceived(object? sender, byte[] simulatedPacket)
        {
            totalPackets++;
            var framer = new PacketFramer(new UartOptions());
            var packets = new List<byte[]>();
            framer.Feed(simulatedPacket, packets);

            var opt = new UartOptions();

            foreach (var raw in packets)
            {
                // Kiểm tra độ dài packet hợp lệ
                if (raw.Length == opt.ExpectedDecodedLength && raw[0] == opt.Header && raw[raw.Length - 1] == opt.Eop)
                {
                    // Kiểm tra CRC giống WorkerAsync
                    ushort received = opt.CrcLittleEndian
                        ? BinaryPrimitives.ReadUInt16LittleEndian(raw.AsSpan(opt.CrcOffset, 2))
                        : BinaryPrimitives.ReadUInt16BigEndian(raw.AsSpan(opt.CrcOffset, 2));

                    ushort computed = Crc16Ciit.Compute(raw.AsSpan(1, opt.CrcCount));
                    bool crcOk = received == computed;

                    if (crcOk)
                    {
                        // Console.WriteLine($"Rec: {string.Join(", ", raw.Select(b => b.ToString("X2")))}");
                        // Lấy phần dữ liệu frame (160 bytes)
                        byte[] frameRaw = new byte[160];
                        Array.Copy(raw, 2, frameRaw, 0, 160);
                        // print frameRaw in hex for debug
                        // Console.WriteLine($"Rec frame: {string.Join(", ", frameRaw.Select(b => b.ToString("X2")))}");
                        short[][] frame = DecodeFrame(frameRaw);
                        // print frame in hex for debug
                        // Console.WriteLine($"Rec frame decoded: {string.Join(", ", frame.SelectMany(g => g).Select(v => v.ToString("X4")))}");
                        SimulatedDataReceived?.Invoke(this, frame);
                    }
                    else
                    {
                        packetErrorCount++;
                        Console.WriteLine($"[SimulateViewModel] CRC error in simulated packet. Total errors: {packetErrorCount}");
                    }
                }
            }
        }

        // Hàm decode giống SerialViewModel
        private short[][] DecodeFrame(byte[] raw)
        {
            int channelCount = 8;
            int samples = raw.Length / (channelCount * 2);  // 160/(8*2)=10

            short[][] frame = new short[channelCount][];

            for (int ch = 0; ch < channelCount; ch++)
            {
                frame[ch] = new short[samples];
                for (int i = 0; i < samples; i++)
                {
                    int idx = (ch * 2) + (i * 16);
                    frame[ch][i] = (short)(raw[idx + 1] | (raw[idx] << 8));
                }
            }

            return frame;
        }

    }
}