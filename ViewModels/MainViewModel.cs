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

    private readonly System.Timers.Timer renderTimer = new(100);

    private volatile bool pendingUpdate;

    // Sự kiện để yêu cầu render lại từ View
    public event Action? OnRequestRender;

    public MainViewModel(ISerialPortService serialPortService)
    {
        
        Serial = new SerialViewModel(serialPortService);
        Osc = new OscilloscopeViewModel();
        Button = new ButtonViewModel();
        Scroll = new ScrollViewModel();

        Scroll.OffsetChanged += offset =>
        {
            Osc.ReadWindow(offset);
            OnRequestRender?.Invoke();
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
                Serial.StartCollectingData();
            }
            else
            {
                pendingUpdate = false;
                Scroll.IsAutoScroll = true;   // cho phép dùng ScrollBar
                Scroll.SetMax(Scroll.CurrentOffset);
                Serial.StopCollectingData();
            }
        };

        Serial.FrameReceived += (s, frame) =>
        {
            // Console.WriteLine("Frame received. Packets received: " + Serial.PacketReceivedCount + ", Packets errored: " + Serial.PacketErrorCount);
            Osc.AppendFrame(frame);
            pendingUpdate = true;
        };
        renderTimer.Start();
    }

}
