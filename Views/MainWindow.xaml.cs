using ScottPlot.Plottables;
using System.Windows;
using OscilloscopeApp.ViewModels;
using OscilloscopeApp.Services;

namespace OscilloscopeApp.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DataStreamer[] _streamers = new DataStreamer[8];

    public MainWindow()
    {
        InitializeComponent();

        // Khởi tạo ViewModel và gán DataContext
        _vm = new MainViewModel(new SerialPortService());
        DataContext = _vm;

        // Khởi tạo đồ thị ScottPlot
        InitPlot();

        // Lắng nghe yêu cầu vẽ lại từ ViewModel
        _vm.OnRequestRender += () =>
        {
            Dispatcher.Invoke(RenderPlot);
        };

        _vm.Osc.ChannelColorChanged += (ch, color) =>
        {
            _streamers[ch].Color = color;
        };
        
        _vm.Osc.RequestRender += () =>
        {
            long offset = _vm.Scroll.CurrentOffset;
            _vm.Osc.ReadWindow(offset);
            RenderPlot();
        };

        // Lắng nghe event RequestColorPicker cho từng ChannelConfigViewModel
        foreach (var cfg in _vm.Osc.ChannelConfigs)
        {
            cfg.RequestColorPicker += OnRequestColorPicker;
        }
    }

    private void OnRequestColorPicker(ChannelConfigViewModel cfg)
    {
        var dlg = new ColorDialog();
        dlg.Color = System.Drawing.ColorTranslator.FromHtml(cfg.ColorHex);
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string hex = $"#{dlg.Color.R:X2}{dlg.Color.G:X2}{dlg.Color.B:X2}";
            cfg.SetColorHex(hex);
        }
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
            _streamers[i].Color = ScottPlot.Color.FromHex(_vm.Osc.ChannelConfigs[i].ColorHex);
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
        long offset = Math.Max(0, _vm.Osc.MaxOffset - 20000);
        Plot.Plot.Axes.SetLimitsX(0, 20000);
        Plot.Refresh();
    }
   
}


