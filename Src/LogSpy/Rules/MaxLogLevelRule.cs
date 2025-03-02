using Microsoft.Extensions.Logging;

namespace LogSpy.Rules;

public class MaxLogLevelRule : ILogRule
{
    private readonly LogLevel _maxAllowedLevel;

    public MaxLogLevelRule(LogLevel maxAllowedLevel)
    {
        _maxAllowedLevel = maxAllowedLevel;
    }

    public bool IsViolatedBy(LogEntry entry)
    {
        return entry.LogLevel > _maxAllowedLevel;
    }

    public string ViolationMessage =>
        $"Log level exceeded the maximum allowed level of '{_maxAllowedLevel}'.";
}
