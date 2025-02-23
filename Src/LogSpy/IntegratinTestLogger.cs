using Microsoft.Extensions.Logging;

namespace LogSpy;

internal class IntegratinTestLogger : ILogger
{
    private readonly bool _isScopeLoggingEnabled;
    private readonly string _categoryName;
    private readonly LogLevel _minLogLevel;
    private readonly LogCaptureService _captureService;
    private readonly Stack<string> _scopes;
    private readonly Action<string> _logAction;

    public IntegratinTestLogger(
        bool isScopeLoggingEnabled,
        string categoryName,
        LogLevel minLogLevel,
        LogCaptureService captureService,
        Stack<string> scopes,
        Action<string> logAction)
    {
        _isScopeLoggingEnabled = isScopeLoggingEnabled;
        _categoryName = categoryName;
        _minLogLevel = minLogLevel;
        _captureService = captureService;
        _logAction = logAction;
        _scopes = scopes;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        var scopeStack = _scopes;

        string scopeString;
        if (state is IDictionary<string, object> dictState)
        {
            scopeString = string.Join(", ", dictState.Select(p => $"{p.Key}: {p.Value}"));
        }
        else
        {
            scopeString = state?.ToString();
        }

        scopeStack.Push(scopeString);

        return new DisposableScope(() =>
        {
            if (scopeStack.Count > 0)
            {
                scopeStack.Pop();
            }
        });
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLogLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var scopeStack = _scopes ?? new Stack<string>();
        var scopes = _isScopeLoggingEnabled
            ? scopeStack.Reverse().ToArray() // oldest scope first
            : Array.Empty<string>();

        var message = formatter != null
            ? formatter(state, exception)
            : state.ToString();

        // Enqueue for later assertions
        LogEntry entry = new LogEntry
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = message,
            Exception = exception,
            Category = _categoryName,
            Scopes = scopes
        };
        _captureService.AddEntry(entry);

        // Also write to action sink if available
        _logAction?.Invoke(
            FormatMessageForOutput(logLevel, message, scopes, exception));
    }

    private string FormatMessageForOutput(
        LogLevel logLevel,
        string message,
        IEnumerable<string> scopes,
        Exception exception)
    {
        var scopeInfo = scopes.Any()
            ? $"{Environment.NewLine}Scopes: {string.Join(" => ", scopes)}{Environment.NewLine}"
            : string.Empty;

        var exInfo = exception != null
            ? $"{Environment.NewLine}Exception: {exception}"
            : string.Empty;

        return $"[{logLevel}] ({_categoryName}) {message}{scopeInfo}{exInfo}";
    }

    private class DisposableScope : IDisposable
    {
        private readonly Action _disposeAction;
        public DisposableScope(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            _disposeAction?.Invoke();
        }
    }
}
