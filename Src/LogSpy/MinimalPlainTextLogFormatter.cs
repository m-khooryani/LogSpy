using System.Text;

namespace LogSpy;

public class MinimalPlainTextLogFormatter : ILogFormatter
{
    public string Format(LogEntry entry)
    {
        var sb = new StringBuilder();

        // e.g. [Info] (MyCategory) Payment success
        sb.Append($"[{entry.LogLevel}] ({entry.Category}) {entry.Message}");

        // Just add correlation if present
        if (!string.IsNullOrWhiteSpace(entry.CorrelationId))
        {
            sb.Append($" [Corr={entry.CorrelationId}]");
        }

        // If there's an exception, put on a new line
        if (entry.Exception != null)
        {
            sb.AppendLine();
            sb.Append($"Exception: {entry.Exception}");
        }

        return sb.ToString();
    }
}

