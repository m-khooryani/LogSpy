using System.Collections.Concurrent;

namespace LogSpy;

public class LogCaptureService : ILogCaptureService
{
    private readonly ConcurrentQueue<LogEntry> _entries = new ConcurrentQueue<LogEntry>();

    public LogCaptureService()
    {
        _entries = new ConcurrentQueue<LogEntry>();
    }

    public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();

    public void AddEntry(LogEntry entry)
    {
        _entries.Enqueue(entry);
    }

    public void Clear()
    {
        _entries.Clear();
    }
}
