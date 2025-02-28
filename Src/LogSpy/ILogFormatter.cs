namespace LogSpy;

public interface ILogFormatter
{
    string Format(LogEntry entry);
}
