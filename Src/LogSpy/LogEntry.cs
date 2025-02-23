using Microsoft.Extensions.Logging;

namespace LogSpy;

public record LogEntry
{
    public LogLevel LogLevel { get; init; }
    public EventId EventId { get; init; }
    public string Message { get; init; }
    public Exception Exception { get; init; }
    public string Category { get; init; }
    public IReadOnlyList<string> Scopes { get; init; }
}
