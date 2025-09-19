using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Forms;


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

    public double Scale { get; set; }
    
    public double Offset { get; set; }

    public System.Windows.Media.Brush ColorBrush => new SolidColorBrush(
    System.Windows.Media.Color.FromArgb(Color.A, Color.R, Color.G, Color.B));

    public ChannelConfigViewModel(int index, string defaultColor)
    {
        ChannelIndex = index;
        colorHex = defaultColor;
        offset = 0;
        scale = 1;
    }
    [RelayCommand]
    public void PickColor()
    {
        var dlg = new ColorDialog();
        dlg.Color = System.Drawing.Color.FromArgb(Color.A, Color.R, Color.G, Color.B);

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            Color = new ScottPlot.Color(dlg.Color.R, dlg.Color.G, dlg.Color.B);
            OnPropertyChanged(nameof(ColorBrush));
        }
    }
}
