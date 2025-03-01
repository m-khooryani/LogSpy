using Xunit.Abstractions;

namespace LogSpy.IntegrationTests;

public class TestOutputSink : ILogSink
{
    private readonly ITestOutputHelper _testOutput;

    public TestOutputSink(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    public void Dispose()
    {
    }

    public void Write(string message)
    {
        _testOutput.WriteLine(message);
    }
}
