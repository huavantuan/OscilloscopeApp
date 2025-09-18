public interface ISerialPortService
{
    bool IsOpen { get; }
    string[] GetPortNames();
    void Open(string portName, int baudRate);
    void Close();
    event EventHandler<byte[]> DataReceived;
}
