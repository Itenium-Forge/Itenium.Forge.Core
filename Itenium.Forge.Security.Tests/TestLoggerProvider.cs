using Microsoft.Extensions.Logging;

namespace Itenium.Forge.Security.Tests;

/// <summary>
/// Captures log messages during tests so assertions can verify warnings are emitted.
/// </summary>
internal class TestLoggerProvider(List<string> messages) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new TestLogger(messages);
    public void Dispose() { }
}

internal class TestLogger(List<string> messages) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => messages.Add($"[{logLevel}] {formatter(state, exception)}");
}
