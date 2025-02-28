using Microsoft.Extensions.Logging;

namespace LogSpy;

public static class LogCaptureExtensions
{
    public static IEnumerable<LogEntry> GetByLevel(
        this LogCaptureService capture,
        LogLevel level)
    {
        return capture.Entries.Where(e => e.LogLevel == level);
    }

    public static IEnumerable<LogEntry> GetByMessageContains(
        this LogCaptureService capture,
        string substring,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        return capture.Entries.Where(e =>
            e.Message != null && e.Message.IndexOf(substring, comparison) >= 0
        );
    }

    public static IEnumerable<LogEntry> GetByCategory(
        this LogCaptureService capture,
        string categoryName)
    {
        return capture.Entries.Where(e =>
            e.Category?.Equals(categoryName, StringComparison.OrdinalIgnoreCase) == true
        );
    }

    public static IEnumerable<LogEntry> GetByTimestampRange(
        this LogCaptureService capture,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        return capture.Entries.Where(e => e.Timestamp >= start && e.Timestamp <= end);
    }

    public static bool HasErrorWithin(
        this LogCaptureService capture,
        TimeSpan window,
        DateTimeOffset referenceTime)
    {
        var cutoff = referenceTime + window;
        return capture.Entries.Any(e =>
            e.LogLevel == LogLevel.Error &&
            e.Timestamp <= cutoff);
    }
}
