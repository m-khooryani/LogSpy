using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace LogSpy.IntegrationTests;

public class WeatherForecastTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly LogCaptureService _logCaptureService;

    public WeatherForecastTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper testOutput)
    {
        factory.SetTestOutputHelper(testOutput); 
        _logCaptureService = factory.GetLogCaptureService();

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Get_WeatherForecast_WritesLogs()
    {
        _logCaptureService.Clear();

        var response = await _client.GetAsync("/weatherforecast/success");

        // Assert
        var entries = _logCaptureService.Entries;
        Assert.Contains(entries, e => e.LogLevel == LogLevel.Information);
        Assert.Contains(entries, e => e.Message.Contains("forecast"));
    }

    [Fact]
    public async Task Get_ErrorSample_WritesErrorLog()
    {
        _logCaptureService.Clear();

        var response = await _client.GetAsync("/weatherforecast/error-sample");

        // Assert
        var entries = _logCaptureService.Entries;
        Assert.DoesNotContain(entries, e => e.LogLevel == LogLevel.Information);
        Assert.Contains(entries, e => e.LogLevel == LogLevel.Error);
    }
}
