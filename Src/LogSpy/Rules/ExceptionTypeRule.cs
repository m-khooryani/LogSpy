namespace LogSpy.Rules;

public class ExceptionTypeRule : ILogRule
{
    private readonly HashSet<Type> _forbiddenExceptions;

    public ExceptionTypeRule(params Type[] forbiddenExceptions)
    {
        _forbiddenExceptions = new HashSet<Type>(forbiddenExceptions);
    }

    public bool IsViolatedBy(LogEntry entry)
    {
        var ex = entry.Exception;
        if (ex == null)
        {
            return false;
        }

        var exType = ex.GetType();
        return _forbiddenExceptions.Any(forbidden => forbidden.IsAssignableFrom(exType));
    }

    public string ViolationMessage =>
        $"An exception of forbidden type was logged: {string.Join(",", _forbiddenExceptions.Select(t => t.Name))}";
}
