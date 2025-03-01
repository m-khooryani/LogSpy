namespace LogSpy;

public interface ILogSink : IDisposable
{
    void Write(string message);
}
