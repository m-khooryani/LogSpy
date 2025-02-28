using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace LogSpy.IntegrationTests;

public class UnitTest1
{
    private ServiceProvider _serviceProvider;
    private LogCaptureService _logCaptureService;
    public ITestOutputHelper Output { get; set; }


    public UnitTest1(ITestOutputHelper output)
    {
        Output = output;
        var services = new ServiceCollection();
        _logCaptureService = new LogCaptureService();
        _logCaptureService.AddRule(new ForbiddenSubstringRule("Forbidden"));

        services.AddSingleton<ClassA>();


        services.AddLogging(builder =>
        {
            builder.ClearProviders();
        });

        var loggerFactory = GetLoggerFactory();
        services.AddSingleton<ILoggerFactory>(loggerFactory);

        services.AddSingleton(provider =>
        {
            var customLoggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return customLoggerFactory.CreateLogger("IntegrationTest");
        });

        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    [Fact]
    public void Test1()
    {
        var service = _serviceProvider.GetRequiredService<ClassA>();
        service.DoSth();

        var violations = _logCaptureService.Violations;
        Assert.True(violations.Count == 0,
            $"Some log rule was violated: {string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void MyTest_WithCorrelation()
    {
        using (CorrelationContext.BeginCorrelationScope("TestId-XYZ"))
        {
            // Create your service provider or logger
            var captureService = new LogCaptureService();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new SpyLoggerProvider(
                    false,
                    captureService,
                    new Dictionary<string, LogLevel> { { "Default", LogLevel.Debug } }
                ));
            });

            var logger = loggerFactory.CreateLogger("MyTestLogger");

            // Act
            logger.LogInformation("Starting operation...");
            logger.LogWarning("Something might be slow.");

            // Assert
            var entries = captureService.Entries;
            // Each entry should have CorrelationId = "TestId-XYZ"
            Assert.All(entries, e => Assert.Equal("TestId-XYZ", e.CorrelationId));
        }
    }

    [Fact]
    public void MyTest_WithActivity()
    {
        var activity = new Activity("MyIntegrationTest")
            .SetIdFormat(ActivityIdFormat.W3C);

        activity.Start();

        try
        {
            // logger config ...
            var capture = new LogCaptureService();
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new SpyLoggerProvider(
                    false,
                    capture,
                    new Dictionary<string, LogLevel> { { "Default", LogLevel.Debug } }
                ));
            });

            var logger = loggerFactory.CreateLogger("MyTestWithActivity");

            logger.LogInformation("Hello from inside an activity");

            // We can check logs
            var entries = capture.Entries;
            Assert.NotEmpty(entries);

            // We expect the first entry to have the same TraceId as activity.TraceId
            var first = entries.First();
            Assert.Equal(activity.TraceId.ToString(), first.TraceId);
        }
        finally
        {
            activity.Stop();
        }
    }

    private LoggerFactory GetLoggerFactory()
    {
        return new LoggerFactory(new[]
        {
            new SpyLoggerProvider(
                false,
                _logCaptureService,
                new Dictionary<string, LogLevel>()
                {
                    { "Default", LogLevel.Information },
                    { "Microsoft", LogLevel.Warning },
                },
                log =>
                {
                    try
                    {
                        Output?.WriteLine(log);
                    }
                    catch
                    {
                    }
                })
        });
    }
}

class ClassA
{
    private readonly ILogger<ClassA> _logger;

    public ClassA(ILogger<ClassA> logger)
    {
        _logger = logger;
    }

    public void DoSth()
    {
        _logger.LogInformation("Doing sth...{Times}", 5);

        _logger.LogInformation($"Forbidden");
    }
}
