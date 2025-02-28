namespace LogSpy;

public class IntegrationTestLoggerOptions
{
    public bool EnableScopes { get; set; } = true;
    public LogOutputFormat OutputFormat { get; set; } = LogOutputFormat.PlainText;
}