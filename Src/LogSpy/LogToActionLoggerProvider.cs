using Microsoft.Extensions.Logging;

namespace LogSpy;

internal class LogToActionLoggerProvider : ILoggerProvider
{
    private readonly bool _isScopeLoggingEnabled;
    private readonly LogCaptureService _captureService;
    private readonly AsyncLocal<Stack<string>> _scopes = new AsyncLocal<Stack<string>>();
    private readonly Action<string> _logAction;
    private readonly LogLevel _defaultLogLevel;
    private readonly IDictionary<string, LogLevel> _logLevels;

    public LogToActionLoggerProvider(
        bool isScopeLoggingEnabled,
        LogCaptureService captureService,
        IDictionary<string, LogLevel> logLevels,
        Action<string> logAction = null)
    {
        _isScopeLoggingEnabled = isScopeLoggingEnabled;
        _captureService = captureService;
        _logAction = logAction; 
        _logLevels = logLevels;
        _defaultLogLevel = logLevels["Default"];
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
            _isScopeLoggingEnabled,
            categoryName,
            minLogLevel,
            _captureService,
            _scopes.Value,
            _logAction);
    }

    public void Dispose()
    {
    }
}
