using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Forms;
using OscilloscopeApp.OscilloscopeViewModels;

namespace OscilloscopeApp.ViewModels;

public partial class ChannelConfigViewModel : ObservableObject
{
    public int ChannelIndex { get; }

    [ObservableProperty]
    private string colorHex;

    [ObservableProperty]
    private double offset;

    [ObservableProperty]
    private double scale;

    public ScottPlot.Color Color { get; set; }

    private OscilloscopeViewModel parent;

    public Brush ColorBrush => new SolidColorBrush(
    (ColorConverter.ConvertFromString(ColorHex) as Color?) ?? Colors.Black);

    public ChannelConfigViewModel(int index, string defaultColor, OscilloscopeViewModel parentViewModel)
    {
        ChannelIndex = index;
        colorHex = defaultColor;
        parent = parentViewModel;
    }

    [RelayCommand]
    public void PickColor()
    {
        var dlg = new ColorDialog();

        // Chuyển từ HEX sang System.Drawing.Color để gán ban đầu
        var currentColor = System.Drawing.ColorTranslator.FromHtml(ColorHex);
        dlg.Color = currentColor;

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            var selected = dlg.Color;

            // Cập nhật lại ColorHex để UI và logic đồng bộ
            ColorHex = $"#{selected.R:X2}{selected.G:X2}{selected.B:X2}";
        }
    }

    public event Action<int>? ScaleChanged;
    partial void OnScaleChanged(double value)
    {
        parent?.UpdateScale(ChannelIndex, value);
        ScaleChanged?.Invoke(ChannelIndex);
    }

    partial void OnOffsetChanged(double value)
    {
        parent?.UpdateOffset(ChannelIndex, value);
    }

    partial void OnColorHexChanged(string value)
    {
        var color = ScottPlot.Color.FromHex(value);
        parent?.RaiseChannelColorChanged(ChannelIndex, color);
        OnPropertyChanged(nameof(ColorBrush));
    }
}
