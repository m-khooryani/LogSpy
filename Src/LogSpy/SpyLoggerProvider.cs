﻿using Microsoft.Extensions.Logging;

namespace LogSpy;

public class SpyLoggerProvider : ILoggerProvider
{
    private readonly IntegrationTestLoggerOptions _options;
    private readonly LogCaptureService _captureService;
    private readonly Stack<string> _scopes = new Stack<string>();
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
        _options = options;
        _captureService = captureService;
        _sink = sink;
        _logLevels = logLevels;
        _defaultLogLevel = logLevels["Default"];
        _formatter = formatter;
    }

    public SpyLoggerProvider(
        LogCaptureService captureService,
        IDictionary<string, LogLevel> logLevels,
        IntegrationTestLoggerOptions options,
        ILogSink? sink = null) : this(
            captureService,
            logLevels,
            options,
            new MinimalPlainTextLogFormatter(),
            sink)
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        var minLogLevel = _defaultLogLevel;
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
            _scopes,
            _options,
            _sink,
            _formatter);
    }

    public void Dispose()
    {
        _sink?.Dispose();
    }
}
