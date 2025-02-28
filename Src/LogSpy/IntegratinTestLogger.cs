using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace LogSpy;

internal class IntegratinTestLogger : ILogger, IDisposable
{
    private readonly IntegrationTestLoggerOptions _isScopeLoggingEnabled;
    private readonly string _categoryName;
    private readonly LogLevel _minLogLevel;
    private readonly LogCaptureService _captureService;
    private readonly Stack<string> _scopes;
    private readonly ILogSink _sink;

    public IntegratinTestLogger(
        string categoryName,
        LogLevel minLogLevel,
        LogCaptureService captureService,
        Stack<string> scopes,
        IntegrationTestLoggerOptions options,
        ILogSink sink)
    {
        _isScopeLoggingEnabled = options;
        _categoryName = categoryName;
        _minLogLevel = minLogLevel;
        _captureService = captureService;
        _scopes = scopes;
        _sink = sink;
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

    public void Dispose()
    {
        (_sink as IDisposable)?.Dispose();
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

        // Check for structured state
        Dictionary<string, object> properties = null;
        if (state is IReadOnlyList<KeyValuePair<string, object>> stateList)
        {
            properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in stateList)
            {
                properties[kv.Key] = kv.Value;
            }
        }

        // Thread & Task info
        var threadId = Thread.CurrentThread.ManagedThreadId;
        var taskId = Task.CurrentId; // null if not in a known task context

        // Correlation
        var correlationId = CorrelationContext.CurrentId;

        // Activity (System.Diagnostics)
        var currentActivity = Activity.Current;
        var traceId = currentActivity?.TraceId.ToString();
        var spanId = currentActivity?.SpanId.ToString();

        // Build message
        var message = formatter?.Invoke(state, exception) ?? state?.ToString();

        // Build final LogEntry with all the extra info
        var logEntry = new LogEntry
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = message,
            Exception = exception,
            Category = _categoryName,
            Scopes = _isScopeLoggingEnabled.EnableScopes ? _scopes.Reverse().ToArray() : Array.Empty<string>(),

            // NEW fields
            CorrelationId = correlationId,
            ThreadId = threadId,
            TaskId = taskId,
            TraceId = traceId,
            SpanId = spanId,
            Properties = properties ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        // Store it in your capture service
        _captureService.AddEntry(logEntry);

        // Also format the text for your console/test output if needed
        _sink?.Write(FormatLogText(logEntry));
    }
    private string FormatLogText(LogEntry entry)
    {
        if (_isScopeLoggingEnabled.OutputFormat == LogOutputFormat.Json)
        {
            // JSON
            return JsonSerializer.Serialize(entry, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
        else
        {
            // Plain text
            var sb = new StringBuilder();
            sb.Append($"[{entry.LogLevel}] ({_categoryName}) {entry.Message}");

            if (!string.IsNullOrEmpty(entry.CorrelationId))
            {
                sb.Append($" | CorrId: {entry.CorrelationId}");
            }

            sb.Append($" | Thread:{entry.ThreadId}");

            if (!string.IsNullOrEmpty(entry.TraceId))
            {
                sb.Append($" | TraceId:{entry.TraceId} SpanId:{entry.SpanId}");
            }

            if (entry.Scopes.Any())
            {
                sb.AppendLine();
                sb.Append($"Scopes: {string.Join(" => ", entry.Scopes)}");
            }

            if (entry.Exception != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ").Append(entry.Exception);
            }

            return sb.ToString();
        }
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
