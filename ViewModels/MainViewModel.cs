using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OscilloscopeApp.OscilloscopeViewModels;
using System.Collections.ObjectModel;
using OscilloscopeApp.Services;
using System.Security.RightsManagement;

public partial class MainViewModel : ObservableObject
{
    public SerialViewModel Serial { get; }
    public OscilloscopeViewModel Osc { get; } = new();
    public ScrollViewModel Scroll { get; } = new();
    private UartReceiverService? uartService;

    private readonly System.Timers.Timer renderTimer = new(33);

    private volatile bool pendingUpdate;

    public event Action? OnRequestRender;

    private CancellationTokenSource? simulateToken;
    private Task? simulateTask;
    private Task? renderTask;

    public bool IsSimulating { get; private set; }

    public string SimulateButtonText => IsSimulating ? "Stop" : "Simulate";

    private bool CommIsConnected = false;
    public string ConnectButtonText => CommIsConnected ? "Disconnect" : "Connect";
    public string ConnectButtonColor => CommIsConnected ? "Red" : "Green";

    private CancellationTokenSource? renderTokenSource;

    public MainViewModel(ISerialPortService serialPortService)
    {

        Scroll.OffsetChanged += offset =>
        {
            Osc.ReadWindow(offset);
            pendingUpdate = true;
        };

        renderTimer.Elapsed += (s, e) =>
        {
            if (pendingUpdate)
            {
                pendingUpdate = false;
                Scroll.SetMax(Osc.MaxOffset);
                Osc.ReadWindow(Scroll.CurrentOffset);
                OnRequestRender?.Invoke();
            }
        };

        Serial = new SerialViewModel(serialPortService);
        Osc = new OscilloscopeViewModel();

        // Scan cổng khi khởi động
        Serial.AvailablePorts.Clear();

        foreach (var port in serialPortService.GetPortNames())
            Serial.AvailablePorts.Add(port);
        renderTimer.Start();
    }

    [RelayCommand]
    private void ConnectButton()
    {
        Console.WriteLine($"ConnectButton called. CommIsConnected: {CommIsConnected}");
        if (uartService == null || !CommIsConnected)
        {
            renderTokenSource?.Cancel();
            renderTokenSource = new CancellationTokenSource();

            var token = renderTokenSource.Token;
            renderTask = Task.Run(() => RunRenderLoop(token), token);
            // Khởi tạo và mở cổng
            if (string.IsNullOrEmpty(Serial.SelectedPort) || !Serial.AvailablePorts.Contains(Serial.SelectedPort))
            {
                // Có thể hiển thị thông báo lỗi hoặc return
                Console.WriteLine("Selected port is invalid or not available.");
                return;
            }
            uartService = new UartReceiverService(
            Serial.SelectedPort,
            int.TryParse(Serial.BaudRate, out var br) ? br : 9600
        );
            uartService.PacketReceived += OnPacketReceived;
            uartService.Start();
            CommIsConnected = true;
            Scroll.IsAutoScroll = false;
        }
        else
        {
            uartService.Stop();
            uartService.PacketReceived -= OnPacketReceived;
            uartService.Dispose();
            uartService = null;
            CommIsConnected = false;
            Scroll.IsAutoScroll = true;   // cho phép dùng ScrollBar
        }

        Serial.ToggleConnectionState();
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(ConnectButtonColor));
        
    }

    private void OnPacketReceived(object? sender, Packet e)
    {
        if (e.CrcOk && e.Raw.Length == 165)
        {
            var data = new byte[160];
            System.Buffer.BlockCopy(e.Raw, 2, data, 0, 160);

            // Chuyển 160 bytes thành 8 kênh, mỗi kênh 10 mẫu 16-bit (short)
            short[][] framePerChannel = new short[8][];
            for (int ch = 0; ch < 8; ch++)
            {
                framePerChannel[ch] = new short[10];
                for (int i = 0; i < 10; i++)
                {
                    int idx = (ch * 2) + (i * 16);
                    framePerChannel[ch][i] = BitConverter.ToInt16(data, idx);
                }
            }

            Osc.AppendFrame(framePerChannel);
        }
        else
        {
            // TODO: log lỗi nếu cần
        }
    }

    private void RunRenderLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Scroll.CurrentOffset = Osc.MaxOffset - Osc.Length;
            Osc.ReadWindow(Scroll.CurrentOffset);
            OnRequestRender?.Invoke();
            Thread.Sleep(33); // ~30 FPS
        }
    }
}
