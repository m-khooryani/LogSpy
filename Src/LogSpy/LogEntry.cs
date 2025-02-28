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
    public Dictionary<string, object> Properties { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public string CorrelationId { get; init; }
    public int ThreadId { get; init; }
    public int? TaskId { get; init; }
    public string TraceId { get; init; }
    public string SpanId { get; init; }
}
