# LogSpy

A flexible **integration-test** logging framework for .NET, allowing you to:
- Capture logs in memory for test assertions
- Apply rules to fail tests if certain messages appear
- Output logs to multiple “sinks” (console, file, test framework, etc.)
- Optionally capture scopes, correlation IDs, structured properties, and more

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
  - [1. Configure in Your Tests](#1-configure-in-your-tests)
  - [2. Capture Logs and Assert](#2-capture-logs-and-assert)
  - [3. (Optional) Add Log Rules](#3-optional-add-log-rules)
- [Advanced Topics](#advanced-topics)
  - [Multiple Output Sinks](#multiple-output-sinks)
  - [Fluent Assertions Integration](#fluent-assertions-integration)
  - [Scopes, Correlation, and Activities](#scopes-correlation-and-activities)
- [Using Dependency Injection](#using-dependency-injection)
  - [Example with a WebApplicationFactory](#example-with-a-webapplicationfactory)
- [Built-In Rules](#built-in-rules)
- [Defining a Custom Rule](#defining-a-custom-rule)
- [Core Interfaces](#core-interfaces)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- **In-Memory Log Capture**: Easily store all logs in a `LogCaptureService` for test-time assertions.
- **Flexible Log Rules**: Fail tests automatically if logs contain forbidden substrings, match regex patterns, or exceed certain levels.
- **Structured Logging Support**: Parse and store structured properties from `ILogger.Log` calls.
- **Scope & Correlation**: Optionally track logger scopes, correlation IDs, thread IDs, or `Activity` data for each log entry.
- **Pluggable Output**: Print logs to console, test framework (e.g., xUnit), file, or multiple destinations at once.
- **JSON or Plain Text Formatting**: Choose how to format logs for external sinks.

Note: This library is aimed at **test** environments, not necessarily high-performance production logging.

---

## Installation

Use NuGet to install **LogSpy**:

```bash
dotnet add package LogSpy
```

Or edit your `.csproj` directly:

```xml
<PackageReference Include="LogSpy" Version="1.0.0" />
```

---

## Quick Start

### 1. Configure in Your Tests

First, create a `LogCaptureService` and build a logger using our `IntegrationTestLoggerProvider`. You may also pick an output sink (like `ConsoleSink`) and a formatter (like `PlainTextLogFormatter`):

```csharp
[Fact]
public void ExampleTest()
{
    // 1) Create the capture
    var capture = new LogCaptureService();

    // 2) Pick a sink (console, file, or test output) and a formatter (plain text or JSON)
    ILogSink sink = new ConsoleSink(); 
    ILogFormatter formatter = new PlainTextLogFormatter();

    // 3) Build the provider
    var provider = new IntegrationTestLoggerProvider(
        capture,
        sink,
        new IntegrationTestLoggerOptions
        {
            EnableScopes = true,
            MinimumLevel = LogLevel.Debug
        },
        formatter
    );

    // 4) Create a logger (via LoggerFactory)
    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.ClearProviders();
        builder.AddProvider(provider);
    });

    var logger = loggerFactory.CreateLogger("MyCategory");

    // 5) Use the logger...
    logger.LogInformation("User #42 logged in successfully");

    // 6) Assert logs
    Assert.Contains(capture.Entries, e => 
        e.Message.Contains("logged in") &&
        e.LogLevel == LogLevel.Information
    );
}
```

### 2. Capture Logs and Assert

After your code under test runs, you can directly check `capture.Entries`. This is vital for verifying logs:

```csharp
Assert.Contains(capture.Entries, e => 
    e.LogLevel == LogLevel.Error && 
    e.Message.Contains("SomethingWentWrong")
);
```

### 3. (Optional) Add Log Rules

Fail tests automatically if certain logs appear:

```csharp
capture.AddRule(new ForbiddenSubstringRule("Forbidden"));
capture.AddRule(new MaxLogLevelRule(LogLevel.Warning)); // no error logs allowed
```

You can configure whether to throw immediately or accumulate violations for a final check:

```csharp
var violations = capture.Violations;
Assert.Empty(violations);
```

---

## Advanced Topics

### Multiple Output Sinks

If you want to log to multiple destinations at once (e.g., console + test output + file), use a `CompositeSink`:

```csharp
var compositeSink = new CompositeSink(
    new ConsoleSink(),
    new TestOutputSink(testOutputHelper),
    new FileSink("TestLogs.txt")
);
```

Logs fan out to all of them.

### Fluent Assertions Integration

You can use Fluent Assertions for more expressive checks:

```csharp
capture.Entries
    .Should()
    .Contain(e => e.LogLevel == LogLevel.Error && e.Message.Contains("Oops"));
```

Or define custom extension methods like `ShouldContainLog(...)`.

### Scopes, Correlation, and Activities

Set `EnableScopes = true` to capture scope data:

- `CorrelationId` from an AsyncLocal (e.g., `CorrelationContext`).
- `TraceId` / `SpanId` from `Activity.Current`.
- `ThreadId` / `TaskId` from the environment.

This helps you see exactly which test or which request produced each log line.

---

## Using Dependency Injection

You can register `ILoggerFactory` in your .NET Core DI container and plug in our **IntegrationTestLoggerProvider**. For instance, in a test project:

```csharp
public IServiceProvider ConfigureServices()
{
    // Create the .NET service collection
    var services = new ServiceCollection();

    // Add standard logging
    services.AddLogging();

    // Suppose you have a method that builds a custom logger factory with LogSpy:
    var loggerFactory = GetCustomLoggerFactory(); // e.g., create with IntegrationTestLoggerProvider

    // Register your custom logger factory in DI
    services.AddSingleton<ILoggerFactory>(loggerFactory);

    // Optionally, register a logger instance for integration tests
    services.AddSingleton(provider =>
    {
        var customFactory = provider.GetRequiredService<ILoggerFactory>();
        return customFactory.CreateLogger("IntegrationTest");
    });

    // Add other services needed by your SUT
    services.AddTransient<ISomeService, SomeServiceImpl>();

    return services.BuildServiceProvider();
}
```

Where `GetCustomLoggerFactory()` is something like:

```csharp
private ILoggerFactory GetCustomLoggerFactory()
{
    var capture = new LogCaptureService();
    var sink = new ConsoleSink();
    var formatter = new PlainTextLogFormatter();

    var provider = new IntegrationTestLoggerProvider(
        capture,
        sink,
        new IntegrationTestLoggerOptions
        {
            EnableScopes = true,
            MinimumLevel = LogLevel.Debug
        },
        formatter
    );

    // Build an ILoggerFactory that uses our custom provider
    return LoggerFactory.Create(builder =>
    {
        builder.ClearProviders();
        builder.AddProvider(provider);
    });
}
```

### Example with a WebApplicationFactory

If you’re using **ASP.NET Core** integration tests with `WebApplicationFactory<TEntryPoint>`, you can also override `CreateHostBuilder` or `ConfigureServices` to integrate LogSpy. For example:

```csharp
public class MyWebAppFactory : WebApplicationFactory<Program> 
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // remove default logging providers
            services.RemoveAll<ILoggerProvider>();

            // add a custom IntegrationTestLoggerProvider
            var capture = new LogCaptureService();
            var formatter = new PlainTextLogFormatter();
            var sink = new ConsoleSink();

            services.AddSingleton<ILoggerProvider>(sp =>
                new IntegrationTestLoggerProvider(
                    capture,
                    sink,
                    new IntegrationTestLoggerOptions
                    {
                        EnableScopes = true,
                        MinimumLevel = LogLevel.Debug
                    },
                    formatter
                )
            );

            // Register the LogCaptureService for assertion later
            services.AddSingleton(capture); 
        });

        return base.CreateHost(builder);
    }
}
```

Then, in your test:

```csharp
[Fact]
public async Task MyIntegrationTest()
{
    // Create the factory, get an HttpClient, etc.
    var factory = new MyWebAppFactory();
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/someEndpoint");
    response.EnsureSuccessStatusCode();

    // Assert logs
    // Because we registered LogCaptureService as a singleton,
    // we can resolve it from the factory's Services:
    var capture = factory.Services.GetRequiredService<LogCaptureService>();

    // Now we can check the logs
    Assert.Contains(capture.Entries, e =>
        e.LogLevel == LogLevel.Information &&
        e.Message.Contains("someEndpoint accessed")
    );
}
```

With this approach, your ASP.NET Core **test server** uses LogSpy for logging, and you can retrieve the same `LogCaptureService` instance from `factory.Services` to assert logs in your test.

---

## Built-In Rules

LogSpy comes with several **built-in** `ILogRule` implementations you can use right away (in addition to `ForbiddenSubstringRule`):

- **RegexLogRule**: Forbid messages matching a certain regex pattern (e.g., secret patterns).
- **MaxLogLevelRule**: Disallow logs above a certain level (e.g., no `Error` logs allowed in a success scenario).
- **ForbiddenCategoryRule**: Disallow logs from certain category names (e.g., `"Microsoft"`, `"System"`).
- **ExceptionTypeRule**: Disallow certain exception types if they appear in logs (e.g., `NullReferenceException`).
- **RepeatedMessageRule**: Forbid the same message repeating more than N times (useful for preventing spammy logs).

You can add these rules similarly:

```csharp
capture.AddRule(new RegexLogRule(@"token=.*"));
capture.AddRule(new ExceptionTypeRule(typeof(NullReferenceException)));
```

Then, depending on your configuration, the test either fails immediately or at the end when you check `capture.Violations`.

---

## Defining a Custom Rule

Implementing a custom `ILogRule` is straightforward. For instance, suppose you want to forbid error-level logs **without** an exception attached:

```csharp
public class ErrorWithoutExceptionRule : ILogRule
{
    public bool IsViolatedBy(LogEntry entry)
    {
        if (entry.LogLevel >= LogLevel.Error && entry.Exception == null)
        {
            return true;
        }
        return false;
    }

    public string ViolationMessage => 
        "Error-level log entry found with no exception attached.";
}
```

Then just add it:

```csharp
capture.AddRule(new ErrorWithoutExceptionRule());
```

Any error-level log lacking an exception triggers a violation.

---

## Core Interfaces

LogSpy provides minimal interfaces for flexibility:

1. **ILogFormatter**  
   ```csharp
   public interface ILogFormatter
   {
       string Format(LogEntry entry);
   }
   ```  
   A simple interface for converting a `LogEntry` into a formatted string (e.g., plain text or JSON).  

2. **ILogRule**  
   ```csharp
   public interface ILogRule
   {
       bool IsViolatedBy(LogEntry entry);
       string ViolationMessage { get; }
   }
   ```  
   A rule that returns `true` if a `LogEntry` violates the rule. Often used for forbidden substrings, levels, exceptions, etc.

3. **ILogSink**  
   ```csharp
   public interface ILogSink : IDisposable
   {
       void Write(string message);
   }
   ```  
   A simple interface for writing logs to an output: console, file, test runner, etc.

Using these interfaces, you can extend LogSpy with new formatters, new sinks, and new rules.

---

## Contributing

1. Fork the repo and create a feature branch.
2. Commit your changes (e.g., `git commit -am "Add some new rule"`).
3. Push to your branch (`git push origin feature/...`) and open a Pull Request.

We’d love additional built-in log rules, sinks, or formatters!

---

## License

Licensed under the [MIT License](LICENSE).  
See the LICENSE file for more details.
