using System.Text;

namespace LogSpy;

public class VerbosePlainTextLogFormatter : ILogFormatter
{
    public string Format(LogEntry entry)
    {
        var sb = new StringBuilder();

        // Example: 2025-02-23T12:34:56.789Z [Information] (MyCategory)
        sb.Append($"{entry.Timestamp:O} ");
        sb.Append($"[{entry.LogLevel}] ");
        sb.Append($"({entry.Category}) ");

        // Main message
        sb.AppendLine(entry.Message);

        // Correlation and Thread Info
        if (!string.IsNullOrWhiteSpace(entry.CorrelationId))
        {
            sb.AppendLine($"CorrId: {entry.CorrelationId}");
        }
        sb.AppendLine($"Thread: {entry.ThreadId} (TaskId: {entry.TaskId?.ToString() ?? "N/A"})");

        // Trace/Span
        if (!string.IsNullOrWhiteSpace(entry.TraceId))
        {
            sb.AppendLine($"TraceId: {entry.TraceId}");
            sb.AppendLine($"SpanId:  {entry.SpanId}");
        }

        // Scopes
        if (entry.Scopes.Any())
        {
            sb.AppendLine("Scopes:");
            foreach (var scope in entry.Scopes)
            {
                sb.AppendLine($"  => {scope}");
            }
        }

        // Exception
        if (entry.Exception != null)
        {
            sb.AppendLine("Exception:");
            sb.AppendLine(entry.Exception.ToString());
        }

        // Optionally, if you want to list structured properties
        if (entry.Properties != null && entry.Properties.Count > 0)
        {
            sb.AppendLine("Properties:");
            foreach (var kv in entry.Properties)
            {
                sb.AppendLine($"  {kv.Key}: {kv.Value}");
            }
        }

        // Add a separator or extra newline for clarity
        sb.AppendLine(new string('-', 50));

        return sb.ToString();
    }
}

