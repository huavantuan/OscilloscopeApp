using CommunityToolkit.Mvvm.ComponentModel;

public partial class OscilloscopeViewModel : ObservableObject
{
    private const int Channels = 8;
    private const int WindowSize = 20000;
    private readonly RingBuffer[] buffers = new RingBuffer[Channels];
    public double[][] DisplayData { get; } = new double[Channels][];
    private readonly short[] tempShort = new short[WindowSize];

    public double[] ScaleFactors { get; } = Enumerable.Repeat(1.0, Channels).ToArray();
    public double[] Offsets { get; } = Enumerable.Repeat(0.0, Channels).ToArray();

    public OscilloscopeViewModel()
    {
        for (int i = 0; i < Channels; i++)
        {
            buffers[i] = new RingBuffer(10_000_000);
            DisplayData[i] = new double[WindowSize];
        }
    }

    public void AppendFrame(ReadOnlySpan<short> interleaved)
    {
        int samplesPerChannel = interleaved.Length / Channels;
        for (int ch = 0; ch < Channels; ch++)
        {
            Span<short> temp = tempShort.AsSpan(0, samplesPerChannel);
            for (int i = 0; i < samplesPerChannel; i++)
                temp[i] = interleaved[i * Channels + ch];
            buffers[ch].Append(temp);
        }
    }

    public void ReadWindow(long offset)
    {
        for (int ch = 0; ch < Channels; ch++)
        {
            Span<short> raw = tempShort.AsSpan();
            buffers[ch].ReadWindow(offset, raw);
            for (int i = 0; i < raw.Length; i++)
                DisplayData[ch][i] = raw[i] * ScaleFactors[ch] + Offsets[ch];
        }
    }

    public long MaxOffset => Math.Max(0, buffers[0].Count - WindowSize);
}
