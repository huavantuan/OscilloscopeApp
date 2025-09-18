public class OscilloscopeViewModel : ObservableObject
{
    public List<SignalPlot> ChannelPlots { get; } = new();

    public void InitializePlot(WpfPlot plot)
    {
        plot.Plot.Clear();
        for (int i = 0; i < 8; i++)
        {
            var signal = plot.Plot.AddSignal(new double[20000]);
            signal.Label = $"Kênh {i + 1}";
            ChannelPlots.Add(signal);
        }
        plot.Refresh();
    }

    public void UpdateData(double[][] channelData)
    {
        for (int i = 0; i < 8; i++)
        {
            ChannelPlots[i].Update(channelData[i]);
        }
    }
}
