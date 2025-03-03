using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace LogSpy.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly LogCaptureService _logCaptureService = new LogCaptureService();
    private ITestOutputHelper _testOutputHelper;

    public void SetTestOutputHelper(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public LogCaptureService GetLogCaptureService() => _logCaptureService;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddLogging();

            var loggerFactory = new LoggerFactory(new[]
            {
                new SpyLoggerProvider(
                    _logCaptureService,
                    new Dictionary<string, LogLevel>
                    {
                        { "Default", LogLevel.Information },
                        { "Microsoft", LogLevel.Warning },
                    },
                    new IntegrationTestLoggerOptions
                    {
                        IsScopesEnabled = false,
                    },
                    new MinimalPlainTextLogFormatter(),
                    new TestOutputSink(_testOutputHelper))
            });

            services.AddSingleton(_logCaptureService);
            services.AddSingleton<ILoggerFactory>(loggerFactory);
            services.AddSingleton(provider =>
                provider.GetRequiredService<ILoggerFactory>().CreateLogger("IntegrationTest"));
        });
    }
}
