public class ScrollViewModel : ObservableObject
{
    public int MaxOffset => 10000000 - 20000;
    private int currentOffset;
    public int CurrentOffset
    {
        get => currentOffset;
        set
        {
            SetProperty(ref currentOffset, value);
            OnOffsetChanged?.Invoke(currentOffset);
        }
    }

    public Action<int> OnOffsetChanged { get; set; }
}
