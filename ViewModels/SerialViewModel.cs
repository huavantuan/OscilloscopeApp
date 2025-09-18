public partial class SerialViewModel : ObservableObject
{
    private readonly ISerialPortService serial;
    public ObservableCollection<string> AvailablePorts { get; } = new();
    [ObservableProperty] private string selectedPort;
    [ObservableProperty] private string baudRate = "115200";
    public string ConnectButtonText => serial.IsOpen ? "Disconnect" : "Connect";

    public SerialViewModel(ISerialPortService serial)
    {
        this.serial = serial;
        foreach (var port in serial.GetPortNames())
            AvailablePorts.Add(port);
    }

    [RelayCommand]
    private void ToggleConnection()
    {
        if (!serial.IsOpen)
            serial.Open(SelectedPort, int.TryParse(BaudRate, out var br) ? br : 115200);
        else
            serial.Close();

        OnPropertyChanged(nameof(ConnectButtonText));
    }
}
