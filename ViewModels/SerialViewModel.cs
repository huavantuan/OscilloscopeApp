using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;
using OscilloscopeApp.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

public partial class SerialViewModel : ObservableObject
{
    private readonly ISerialPortService serial;
    public ObservableCollection<string> AvailablePorts { get; } = new();
    [ObservableProperty] private string selectedPort = "COM1";
    [ObservableProperty] private string baudRate = "115200";

    private bool isConnected = false;
    public string ConnectButtonText => isConnected ? "Disconnect" : "Connect";
    public Brush ConnectButtonColor => isConnected ? Brushes.Red : Brushes.Green;

    public SerialViewModel(ISerialPortService serial)
    {
        this.serial = serial ?? throw new ArgumentNullException(nameof(serial));
        foreach (var port in serial.GetPortNames())
            AvailablePorts.Add(port);

        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(ConnectButtonColor));
    }
    
    public void ToggleConnectionState()
    {
        isConnected = !isConnected;
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(ConnectButtonColor));
    }
}
