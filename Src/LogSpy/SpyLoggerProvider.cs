using Microsoft.Extensions.Logging;

namespace LogSpy;

public class SpyLoggerProvider : ILoggerProvider
{
    private readonly IntegrationTestLoggerOptions _isScopeLoggingEnabled;
    private readonly LogCaptureService _captureService;
    private readonly AsyncLocal<Stack<string>> _scopes = new AsyncLocal<Stack<string>>();
    private readonly ILogSink? _sink;  
    private readonly LogLevel _defaultLogLevel;
    private readonly IDictionary<string, LogLevel> _logLevels;
    private readonly ILogFormatter _formatter;

    public SpyLoggerProvider(
        LogCaptureService captureService,
        IDictionary<string, LogLevel> logLevels,
        IntegrationTestLoggerOptions options,
        ILogFormatter formatter,
        ILogSink? sink = null)
    {
        _isScopeLoggingEnabled = options;
        _captureService = captureService;
        _sink = sink;
        _logLevels = logLevels;
        _defaultLogLevel = logLevels["Default"];
        _formatter = formatter;
    }

    public ILogger CreateLogger(string categoryName)
    {
        var minLogLevel = _defaultLogLevel;
        _scopes.Value = new Stack<string>();
        foreach (var logLevelPair in _logLevels)
        {
            if (categoryName.StartsWith(logLevelPair.Key.TrimEnd('*')))
            {
                minLogLevel = logLevelPair.Value;
                break;
            }
        }

        return new IntegratinTestLogger(
            categoryName,
            minLogLevel,
            _captureService,
            _scopes.Value,
            _isScopeLoggingEnabled,
            _sink,
            _formatter);
    }

    public void Dispose()
    {
    }
}
