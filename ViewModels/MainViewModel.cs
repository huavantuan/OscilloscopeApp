using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Timers;
using OscilloscopeApp.OscilloscopeViewModels;

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

    private CancellationTokenSource? simulateToken;
    private Task? simulateTask;
    private Task? renderTask;

    public bool IsSimulating { get; private set; }

    public string SimulateButtonText => IsSimulating ? "Stop" : "Simulate";

    public MainViewModel(ISerialPortService serial)
    {
        this.serial = serial;
        Serial = new SerialViewModel(serial);

        serial.DataReceived += (s, bytes) =>
        {
            var span = MemoryMarshal.Cast<byte, short>(bytes.AsSpan());
            //Osc.AppendFrame(span);
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
        if (IsSimulating)
        {
            simulateToken?.Cancel();
            IsSimulating = false;
            return;
        }

        simulateToken = new CancellationTokenSource();
        var token = simulateToken.Token;
        IsSimulating = true;

        simulateTask = Task.Run(() => RunSimulation(token));
        renderTask = Task.Run(() => RunRenderLoop(token));
    }
    private void RunSimulation(CancellationToken token)
    {
        const int batchSize = 1000;
        int total = 0;

        while (!token.IsCancellationRequested)
        {
            var frame = new short[8][];
            for (int ch = 0; ch < 8; ch++)
            {
                frame[ch] = new short[batchSize];
                double phase = ch * Math.PI / 9;
                for (int i = 0; i < batchSize; i++)
                    frame[ch][i] = (short)(Math.Sin((total + i) * 0.01 + phase) * 30000);
            }

            Osc.AppendFrame(frame);
            Scroll.CurrentOffset = Osc.MaxOffset - Osc.Length;
            total += batchSize;

            Thread.Sleep(10); // không dùng await ở đây
        }
    }
    private void RunRenderLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Osc.ReadWindow(Scroll.CurrentOffset);
            OnRequestRender?.Invoke();

            Thread.Sleep(33); // ~30 FPS
        }
    }
}
