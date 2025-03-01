using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LogSpy;

internal class IntegratinTestLogger : ILogger, IDisposable
{
    private readonly IntegrationTestLoggerOptions _options;
    private readonly string _categoryName;
    private readonly LogLevel _minLogLevel;
    private readonly LogCaptureService _captureService;
    private readonly Stack<string> _scopes;
    private readonly ILogSink? _sink;
    private readonly ILogFormatter _formatter;

    public IntegratinTestLogger(
        string categoryName,
        LogLevel minLogLevel,
        LogCaptureService captureService,
        Stack<string> scopes,
        IntegrationTestLoggerOptions options,
        ILogSink? sink,
        ILogFormatter formatter)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        _captureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _minLogLevel = minLogLevel;
        _sink = sink;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (!_options.IsScopesEnabled || state == null)
        {
            return NullScope.Instance;
        }

        var scopeString = ConvertScopeStateToString(state);
        _scopes.Push(scopeString);

        return new DisposableScope(() =>
        {
            if (_scopes.Count > 0)
            {
                _scopes.Pop();
            }
        });
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var logEntry = BuildLogEntry(logLevel, eventId, state, exception, formatter);
        _captureService.AddEntry(logEntry);

        var formattedText = _formatter.Format(logEntry);
        _sink?.Write(formattedText);
    }

    public void Dispose()
    {
        if (_sink is IDisposable disposableSink)
        {
            disposableSink.Dispose();
        }
    }

    private LogEntry BuildLogEntry<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        string message = formatter?.Invoke(state, exception) ?? state?.ToString()!;

        return new LogEntry
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = message,
            Exception = exception,
            Category = _categoryName,
            Scopes = _options.IsScopesEnabled
                         ? _scopes.Reverse().ToArray()
                         : [],

            CorrelationId = CorrelationContext.CurrentId,
            ThreadId = Environment.CurrentManagedThreadId,
            TaskId = Task.CurrentId,
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Properties = ExtractStructuredProperties(state)
        };
    }

    private static Dictionary<string, object> ExtractStructuredProperties<TState>(TState state)
    {
        if (state is IReadOnlyList<KeyValuePair<string, object>> kvList)
        {
            return kvList
                .ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    private static string ConvertScopeStateToString<TState>(TState state)
    {
        if (state is IDictionary<string, object> dictState)
        {
            return string.Join(", ", dictState.Select(p => $"{p.Key}: {p.Value}"));
        }
        return state?.ToString() ?? string.Empty;
    }

    private class DisposableScope : IDisposable
    {
        private readonly Action _disposeAction;

        public DisposableScope(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose() => _disposeAction?.Invoke();
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();
        private NullScope() { }
        public void Dispose() { }
    }
}
