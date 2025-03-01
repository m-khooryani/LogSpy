using Microsoft.Extensions.Logging;

namespace LogSpy;

public record LogEntry
{
    public LogLevel LogLevel { get; init; }
    public EventId EventId { get; init; }
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
    public required string Category { get; init; }
    public required IReadOnlyList<string> Scopes { get; init; }
    public required Dictionary<string, object> Properties { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public required string CorrelationId { get; init; }
    public int ThreadId { get; init; }
    public int? TaskId { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
