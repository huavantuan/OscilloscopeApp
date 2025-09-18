public partial class ScrollViewModel : ObservableObject
{
    [ObservableProperty] private long currentOffset;
    public long MaxOffset { get; private set; }
    public event Action<long> OffsetChanged;

    public void SetMax(long max)
    {
        MaxOffset = Math.Max(0, max);
        OnPropertyChanged(nameof(MaxOffset));
        if (CurrentOffset > MaxOffset) CurrentOffset = MaxOffset;
    }

    partial void OnCurrentOffsetChanged(long value) => OffsetChanged?.Invoke(value);
}
