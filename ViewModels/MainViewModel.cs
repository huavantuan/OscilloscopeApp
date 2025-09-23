using CommunityToolkit.Mvvm.ComponentModel;
using OscilloscopeApp.OscilloscopeViewModels;
using OscilloscopeApp.Services;
using OscilloscopeApp.ViewModels;


public partial class MainViewModel : ObservableObject
{
    public SerialViewModel Serial { get; }
    public OscilloscopeViewModel Osc { get; } = new();
    public ScrollViewModel Scroll { get; } = new();
    public ButtonViewModel Button { get; } = new();

    public SimulateViewModel Simulate { get; } = new SimulateViewModel(new SimulatedSerialPortService());


    private readonly System.Timers.Timer renderTimer = new(33);

    private volatile bool pendingUpdate;

    public event Action? OnRequestRender;

    public MainViewModel(ISerialPortService serialPortService)
    {
        
        Serial = new SerialViewModel(serialPortService);
        Osc = new OscilloscopeViewModel();
        Button = new ButtonViewModel();

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
                Scroll.CurrentOffset = Osc.MaxOffset - Osc.Length;
                Osc.ReadWindow(Scroll.CurrentOffset);
                OnRequestRender?.Invoke();
            }
        };

        Button.ButtonConnectedChanged += (s, isConnected) =>
        {
            if (isConnected)
            {
                pendingUpdate = true;
                Scroll.IsAutoScroll = false;  // không cho dùng ScrollBar
                Serial.StartCollectingData();
            }
            else
            {
                pendingUpdate = false;
                Scroll.IsAutoScroll = true;   // cho phép dùng ScrollBar
                Serial.StopCollectingData();
            }
        };

        Serial.FrameReceived += (s, frame) =>
        {
            Console.WriteLine("Frame received. Packets received: " + Serial.PacketReceivedCount + ", Packets errored: " + Serial.PacketErrorCount);
            Osc.AppendFrame(frame);
            pendingUpdate = true;
        };
        renderTimer.Start();

        Simulate.SimulatedDataReceived += (s, data) =>
        {
            // Xử lý dữ liệu giả lập như dữ liệu thật
            Osc.AppendFrame(data);
            pendingUpdate = true;
        };
    }

}
