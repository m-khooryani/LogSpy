namespace LogSpy;

public interface ILogRule
{
    bool IsViolatedBy(LogEntry entry);
    string ViolationMessage { get; }
}
