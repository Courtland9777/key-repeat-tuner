namespace StarCraftKeyManager.Adapters;

public interface IWrappedEventRecordWrittenEventArgs
{
    IWrappedEventRecord? EventRecord { get; }
}