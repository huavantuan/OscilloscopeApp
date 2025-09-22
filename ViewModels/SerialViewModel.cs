using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;
using OscilloscopeApp.Services;
using System.Security.RightsManagement;

namespace OscilloscopeApp.ViewModels;

public partial class SerialViewModel : ObservableObject
{
    private readonly ISerialPortService _serial;
    private UartReceiverService? _uart;

    public ObservableCollection<string> AvailablePorts { get; } = new();
    [ObservableProperty] private string selectedPort = "COM1";
    [ObservableProperty] private string baudRate = "3000000";
    [ObservableProperty] private bool isCollectingStarted = false;
    [ObservableProperty] private long packetReceivedCount = 0;
    [ObservableProperty] private long packetErrorCount = 0;
    // Sự kiện phát ra frame đã decode
    public event EventHandler<short[][]>? FrameReceived;

    public SerialViewModel(ISerialPortService serial)
    {
        _serial = serial ?? throw new ArgumentNullException(nameof(serial));
        foreach (var port in serial.GetPortNames())
            AvailablePorts.Add(port);
        if (AvailablePorts.Count > 0)
            SelectedPort = AvailablePorts[0];
    }

    public void StartCollectingData()
    {
        if (_uart == null)
        {
            // Khởi tạo và mở cổng
            if (string.IsNullOrEmpty(SelectedPort) || !AvailablePorts.Contains(SelectedPort))
            {
                // Có thể hiển thị thông báo lỗi hoặc return
                Console.WriteLine("Selected port is invalid or not available.");
                return;
            }
            _uart = new UartReceiverService(
                SelectedPort,
                int.TryParse(BaudRate, out var br) ? br : 9600
            );
            if (_uart != null)
                _uart.PacketReceived += OnPacketReceived;
            _uart?.Start();
            IsCollectingStarted = true;
        }
    }
    
    public void StopCollectingData()
    {
        if (_uart != null)
        {
            _uart.Stop();
            _uart.PacketReceived -= OnPacketReceived;
            _uart.Dispose();
            _uart = null;
            IsCollectingStarted = false;
        }
    }

    private void OnPacketReceived(object? sender, Packet paket)
    {
        if (paket.CrcOk && paket.Raw.Length == 165)
        {
            PacketReceivedCount++;
            var data = new byte[160];
            System.Buffer.BlockCopy(paket.Raw, 2, data, 0, 160);

            // Chuyển 160 bytes thành 8 kênh, mỗi kênh 10 mẫu 16-bit (short)
            short[][] frame = DecodeFrame(data);
            FrameReceived?.Invoke(this, frame);
        }
        else
        {
            PacketErrorCount++;
        }
    }

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
                frame[ch][i] = BitConverter.ToInt16(raw, idx);
            }
        }

        return frame;
    }

}
