public class SerialViewModel : ObservableObject
{
    public ObservableCollection<string> AvailablePorts { get; } = new(SerialPort.GetPortNames());
    public string SelectedPort { get; set; } = "COM1";
    public string BaudRate { get; set; } = "9600";

    private bool isConnected = false;
    public string ConnectButtonText => isConnected ? "Disconnect" : "Connect";

    [RelayCommand]
    private void ToggleConnection()
    {
        isConnected = !isConnected;
        OnPropertyChanged(nameof(ConnectButtonText));
        // TODO: Gọi thư viện giao tiếp sau
    }
}
