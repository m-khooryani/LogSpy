using Microsoft.Extensions.Logging;

namespace LogSpy.Rules;

public class ErrorWithoutExceptionRule : ILogRule
{
    private readonly LogLevel _minErrorLevel;

    public ErrorWithoutExceptionRule(LogLevel minErrorLevel = LogLevel.Error)
    {
        _minErrorLevel = minErrorLevel;
    }

    public bool IsViolatedBy(LogEntry entry)
    {
        if (entry.LogLevel < _minErrorLevel)
        {
            return false;
        }

        // If at or above minErrorLevel but exception is null => violation
        return entry.Exception == null;
    }

    public string ViolationMessage =>
        $"Error-level log without an attached exception (level >= {_minErrorLevel}).";
}
