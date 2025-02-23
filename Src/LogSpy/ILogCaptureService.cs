namespace LogSpy;

public interface ILogCaptureService
{
    IReadOnlyCollection<LogEntry> Entries { get; }
    void AddEntry(LogEntry entry);
    void Clear();
}
