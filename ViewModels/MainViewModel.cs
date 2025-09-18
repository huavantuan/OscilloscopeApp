public partial class MainViewModel : ObservableObject
{
    public SerialViewModel Serial { get; } = new();
    public OscilloscopeViewModel Oscilloscope { get; } = new();
    public ScrollViewModel Scroll { get; } = new();

    public MainViewModel()
    {
        Scroll.OnOffsetChanged = offset =>
        {
            var data = LoadData(offset); // TODO: lấy dữ liệu từ file/bộ nhớ
            Oscilloscope.UpdateData(data);
        };
    }

    private double[][] LoadData(int offset)
    {
        // Tạo dữ liệu giả để test
        var result = new double[8][];
        for (int i = 0; i < 8; i++)
        {
            result[i] = Enumerable.Range(0, 20000).Select(x => Math.Sin(2 * Math.PI * x / 1000.0 + i)).ToArray();
        }
        return result;
    }
}
