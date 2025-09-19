using ScottPlot.Plottables;
using System.Windows;
using OscilloscopeApp.ViewModels;

namespace OscilloscopeApp.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DataStreamer[] _streamers = new DataStreamer[8];

    public MainWindow()
    {
        InitializeComponent();

        // Khởi tạo ViewModel và gán DataContext
        _vm = new MainViewModel(new MockSerialPortService());
        DataContext = _vm;

        // Khởi tạo đồ thị ScottPlot
        InitPlot();

        // Lắng nghe yêu cầu vẽ lại từ ViewModel
        _vm.OnRequestRender += () =>
        {
            Dispatcher.Invoke(RenderPlot);
        };
    }

    private void InitPlot()
    {
        Plot.Plot.Clear();

        for (int i = 0; i < 8; i++)
        {
            // Tạo DataStreamer cho mỗi kênh
            _streamers[i] = Plot.Plot.Add.DataStreamer(20000);
            _streamers[i].LegendText = $"Kênh {i + 1}";
            _streamers[i].ManageAxisLimits = false;
            _streamers[i].LineWidth = 1;
            _streamers[i].Color = ScottPlot.Color.FromHex("#c5047bff");
        }

        // Plot.Plot.LegendShowItemsFromHiddenPlottables();
        Plot.Plot.Axes.SetLimitsY(-32768, 32767); // nếu dùng Int16 gốc
        Plot.Refresh();
    }

    private void RenderPlot()
    {
        for (int i = 0; i < 8; i++)
        {
            _streamers[i].Clear();
            foreach (var value in _vm.Osc.DisplayData[i])
                _streamers[i].Add(value);
        }

        // long offset = _vm.Scroll.CurrentOffset;
        long offset = Math.Max(0, _vm.Osc.MaxOffset - 20000);
        Plot.Plot.Axes.SetLimitsX(0, 20000);
        Plot.Refresh();
    }
   
}


