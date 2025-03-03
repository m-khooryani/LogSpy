using Xunit.Abstractions;

namespace LogSpy.IntegrationTests;

public class TestOutputSink : ILogSink
{
    private readonly ITestOutputHelper _testOutput;

    public TestOutputSink(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput ?? throw new ArgumentNullException(nameof(testOutput));
    }

    public void Dispose()
    {
    }

    public void Write(string message)
    {
        try
        {
            _testOutput.WriteLine(message);
        }
        catch
        {
        }
    }
}
