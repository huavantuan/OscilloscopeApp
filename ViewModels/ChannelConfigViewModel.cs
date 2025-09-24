using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using OscilloscopeApp.OscilloscopeViewModels;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;

namespace OscilloscopeApp.ViewModels;

public partial class ChannelConfigViewModel : ObservableObject
{
    public int ChannelIndex { get; }

    [ObservableProperty] private string colorHex;

    [ObservableProperty] private double offset;

    [ObservableProperty] private double scale;

    public ScottPlot.Color Color { get; set; }

    private OscilloscopeViewModel parent;

    public System.Windows.Media.Brush ColorBrush => new SolidColorBrush(
        (MediaColorConverter.ConvertFromString(ColorHex) as MediaColor?) ?? MediaColor.FromRgb(0, 0, 0));

    public ChannelConfigViewModel(int index, string defaultColor, OscilloscopeViewModel parentViewModel)
    {
        ChannelIndex = index;
        ColorHex = defaultColor;
        parent = parentViewModel;

    }

    // MVVM-friendly: raise event để View mở ColorPicker
    public event Action<ChannelConfigViewModel>? RequestColorPicker;

    [RelayCommand]
    public void PickColor()
    {
        // Raise event để View xử lý mở ColorPicker
        RequestColorPicker?.Invoke(this);
    }

    // View gọi hàm này sau khi chọn màu xong
    public void SetColorHex(string hex)
    {
        ColorHex = hex;
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

