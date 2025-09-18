public class MockSerialPortService : ISerialPortService
{
    private readonly System.Timers.Timer timer = new(10);
    public bool IsOpen { get; private set; }
    public event EventHandler<byte[]> DataReceived;

    public string[] GetPortNames() => new[] { "COM1", "COM2", "COM3" };

    public void Open(string portName, int baudRate)
    {
        IsOpen = true;
        timer.Elapsed += (s, e) => EmitFakeData();
        timer.Start();
    }

    public void Close()
    {
        timer.Stop();
        IsOpen = false;
    }

    private void EmitFakeData()
    {
        var data = new byte[8 * 1000 * sizeof(short)];
        var rand = new Random();
        for (int i = 0; i < data.Length; i += 2)
        {
            short val = (short)rand.Next(short.MinValue, short.MaxValue);
            data[i] = (byte)(val & 0xFF);
            data[i + 1] = (byte)((val >> 8) & 0xFF);
        }
        DataReceived?.Invoke(this, data);
    }
}
