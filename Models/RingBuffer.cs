public class RingBuffer
{
    private readonly short[] buffer;
    private long writeIndex;
    public long Count { get; private set; }
    public long Capacity => buffer.LongLength;

    public RingBuffer(long capacity)
    {
        buffer = new short[capacity];
    }

    public void Append(ReadOnlySpan<short> data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            buffer[writeIndex % Capacity] = data[i];
            writeIndex++;
            if (Count < Capacity) Count++;
        }
    }

    public void ReadWindow(long offset, Span<short> dest)
    {
        long length = dest.Length;
        if (offset + length > Count) offset = Count - length;
        if (offset < 0) offset = 0;

        long start = (writeIndex - Count + offset) % Capacity;
        for (int i = 0; i < length; i++)
            dest[i] = buffer[(start + i) % Capacity];
    }
}
