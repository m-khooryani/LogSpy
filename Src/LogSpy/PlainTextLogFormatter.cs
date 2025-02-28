using System.Text;

namespace LogSpy;

public class PlainTextLogFormatter : ILogFormatter
{
    public PlainTextLogFormatter()
    {
    }

    public string Format(LogEntry entry)
    {
        var sb = new StringBuilder();
        sb.Append($"[{entry.LogLevel}] ({entry.Category}) {entry.Message}");

        if (!string.IsNullOrEmpty(entry.CorrelationId))
        {
            sb.Append($" | CorrId: {entry.CorrelationId}");
        }

        sb.Append($" | Thread:{entry.ThreadId}");

        if (!string.IsNullOrEmpty(entry.TraceId))
        {
            sb.Append($" | TraceId:{entry.TraceId} SpanId:{entry.SpanId}");
        }

        if (entry.Scopes.Any())
        {
            sb.AppendLine();
            sb.Append($"Scopes: {string.Join(" => ", entry.Scopes)}");
        }

        if (entry.Exception != null)
        {
            sb.AppendLine();
            sb.Append("Exception: ").Append(entry.Exception);
        }

        return sb.ToString();
    }
}
