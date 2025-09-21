using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using OscilloscopeApp.ViewModels;

namespace OscilloscopeApp.OscilloscopeViewModels;

public partial class OscilloscopeViewModel : ObservableObject
{
    private const int Channels = 8;
    private const int WindowSize = 20000;
    private readonly RingBuffer[] buffers = new RingBuffer[Channels];
    public double[][] DisplayData { get; } = new double[Channels][];
    private readonly short[][] tempShort = new short[Channels][];

    public double[] ScaleFactors { get; } = Enumerable.Repeat(1.0, Channels).ToArray();
    public double[] Offsets { get; } = Enumerable.Repeat(0.0, Channels).ToArray();
    public ObservableCollection<ChannelConfigViewModel> ChannelConfigs { get; }

    public event Action<int, ScottPlot.Color>? ChannelColorChanged;
    public event Action? RequestRender;

    public OscilloscopeViewModel()
    {
        for (int i = 0; i < Channels; i++)
        {
            buffers[i] = new RingBuffer(100_000_000);
            DisplayData[i] = new double[WindowSize];
            tempShort[i] = new short[WindowSize];
        }
        ChannelConfigs = new ObservableCollection<ChannelConfigViewModel>();
        string[] defaultColors = new[]
        {
            "#c5047bff", "#ff5722", "#4caf50", "#2196f3",
            "#ffeb3b", "#9c27b0", "#795548", "#e91e63"
        };

        for (int i = 0; i < 8; i++)
        {
            ChannelConfigs.Add(new ChannelConfigViewModel(i, defaultColors[i], this));

            ChannelConfigs[i].ScaleChanged += (i) =>
            {
                ScaleFactors[i] = ChannelConfigs[i].Scale;
                RequestRender?.Invoke(); // gọi render lại
            };
        }
    }

    public void AppendFrame(short[][] framePerChannel)
    {
        for (int ch = 0; ch < Channels; ch++)
        {
            buffers[ch].Append(framePerChannel[ch]);
        }
    }

    public void ReadWindow(long offset)
    {
        for (int ch = 0; ch < Channels; ch++)
        {
            var raw = tempShort[ch];
            buffers[ch].ReadWindow(offset, raw);

            for (int i = 0; i < raw.Length; i++)
                DisplayData[ch][i] = raw[i] * ScaleFactors[ch] + Offsets[ch];
        }
    }

    public long MaxOffset => buffers.Max(b => b.Count);
    public long Length => WindowSize;

    public void UpdateScale(int channel, double value)
    {
        ScaleFactors[channel] = value;
    }

    public void UpdateOffset(int channel, double value)
    {
        Offsets[channel] = value;
    }
    public void RaiseChannelColorChanged(int channelIndex, ScottPlot.Color color)
    {
        ChannelColorChanged?.Invoke(channelIndex, color);
    }
}
