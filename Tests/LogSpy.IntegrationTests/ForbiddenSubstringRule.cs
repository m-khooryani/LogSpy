namespace LogSpy.IntegrationTests;

public class ForbiddenSubstringRule : ILogRule
{
    private readonly string _substring;
    public ForbiddenSubstringRule(string substring)
    {
        _substring = substring;
    }

    public bool IsViolatedBy(LogEntry entry)
    {
        return entry.Message?.Contains(_substring, StringComparison.OrdinalIgnoreCase) == true;
    }

    public string ViolationMessage => $"Message contains forbidden substring '{_substring}'.";
}