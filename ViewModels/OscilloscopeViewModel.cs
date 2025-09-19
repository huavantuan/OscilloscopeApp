using CommunityToolkit.Mvvm.ComponentModel;

public partial class OscilloscopeViewModel : ObservableObject
{
    private const int Channels = 8;
    private const int WindowSize = 20000;
    private readonly RingBuffer[] buffers = new RingBuffer[Channels];
    public double[][] DisplayData { get; } = new double[Channels][];
    private readonly short[][] tempShort = new short[Channels][];

    public double[] ScaleFactors { get; } = Enumerable.Repeat(1.0, Channels).ToArray();
    public double[] Offsets { get; } = Enumerable.Repeat(0.0, Channels).ToArray();



    public OscilloscopeViewModel()
    {
        for (int i = 0; i < Channels; i++)
        {
            buffers[i] = new RingBuffer(10_000_000);
            DisplayData[i] = new double[WindowSize];
            tempShort[i] = new short[WindowSize];
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
}
