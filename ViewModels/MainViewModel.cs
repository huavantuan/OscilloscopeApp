using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Timers;

namespace OscilloscopeApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public SerialViewModel Serial { get; }
    public OscilloscopeViewModel Osc { get; } = new();
    public ScrollViewModel Scroll { get; } = new();

    private readonly ISerialPortService serial;
    private readonly System.Timers.Timer renderTimer = new(33);
    private volatile bool pendingUpdate;

    public event Action? OnRequestRender;

    public MainViewModel(ISerialPortService serial)
    {
        this.serial = serial;
        Serial = new SerialViewModel(serial);
        
        serial.DataReceived += (s, bytes) =>
        {
            var span = MemoryMarshal.Cast<byte, short>(bytes.AsSpan());
            Osc.AppendFrame(span);
            pendingUpdate = true;
        };

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
        renderTimer.Start();
    }
    [RelayCommand]
    public void Simulate()
    {
        const int pointCount = 20000;
        const double phaseStep = Math.PI / 9; // 20 độ ≈ 0.349 rad
        const double freq = 2 * Math.PI / 1000.0;

        for (int ch = 0; ch < 8; ch++)
        {
            double phase = ch * phaseStep;
            for (int i = 0; i < pointCount; i++)
            {
                Osc.DisplayData[ch][i] = Math.Sin(i * freq + phase) * 30000; // scale về Int16
            }
        }
        Console.WriteLine("Simulate called");
        OnRequestRender?.Invoke();
    }
}
