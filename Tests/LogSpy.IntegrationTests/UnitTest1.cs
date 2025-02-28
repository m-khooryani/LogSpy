using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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
