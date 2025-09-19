using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;

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
        this.serial = serial;
        foreach (var port in serial.GetPortNames())
            AvailablePorts.Add(port);

        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(ConnectButtonColor));
    }

    //[RelayCommand]
    // private void ToggleConnection()
    // {
    //     if (!serial.IsOpen)
    //         serial.Open(SelectedPort, int.TryParse(BaudRate, out var br) ? br : 115200);
    //     else
    //         serial.Close();

    //     OnPropertyChanged(nameof(ConnectButtonText));
    // }
    [RelayCommand]
    private void ConnectButton()
    {
        isConnected = !isConnected;

        // TODO: Gọi mở hoặc đóng cổng thật nếu cần

        // Cập nhật giao diện
        Console.WriteLine("ConnectButton called");
        OnPropertyChanged(nameof(ConnectButtonText));
        OnPropertyChanged(nameof(ConnectButtonColor));
    }
}
