using Itenium.Forge.Core;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Itenium.Forge.Logging.Tests;

[TestFixture]
public class GlobalExceptionHandlerTests
{
    [Test]
    public async Task TryHandleAsync_LogsErrorAndReturnsTrue()
    {
        var logger = new FakeLogger();
        var settings = new ForgeSettings { Environment = "Production" };
        var handler = new GlobalExceptionHandler(logger, settings, new AlwaysTrueProblemDetailsService());

        var context = new DefaultHttpContext();
        var exception = new InvalidOperationException("boom");

        var result = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.That(result, Is.True);
        Assert.That(logger.LoggedErrors, Has.Count.EqualTo(1));
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
    }

    [Test]
    public async Task TryHandleAsync_Development_IncludesExceptionDetail()
    {
        var settings = new ForgeSettings { Environment = "Development" };
        var problemDetails = new CapturingProblemDetailsService();
        var handler = new GlobalExceptionHandler(new FakeLogger(), settings, problemDetails);

        var exception = new InvalidOperationException("dev-detail");
        await handler.TryHandleAsync(new DefaultHttpContext(), exception, CancellationToken.None);

        Assert.That(problemDetails.CapturedContext?.ProblemDetails.Detail, Does.Contain("dev-detail"));
    }

    [Test]
    public async Task TryHandleAsync_Production_OmitsExceptionDetail()
    {
        var settings = new ForgeSettings { Environment = "Production" };
        var problemDetails = new CapturingProblemDetailsService();
        var handler = new GlobalExceptionHandler(new FakeLogger(), settings, problemDetails);

        var exception = new InvalidOperationException("prod-secret");
        await handler.TryHandleAsync(new DefaultHttpContext(), exception, CancellationToken.None);

        Assert.That(problemDetails.CapturedContext?.ProblemDetails.Detail, Is.Null);
    }

    // ---------- helpers ----------

    private sealed class FakeLogger : ILogger<GlobalExceptionHandler>
    {
        public List<string> LoggedErrors { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
                LoggedErrors.Add(formatter(state, exception));
        }
    }

    private sealed class AlwaysTrueProblemDetailsService : IProblemDetailsService
    {
        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context) => ValueTask.FromResult(true);
        public ValueTask WriteAsync(ProblemDetailsContext context) => ValueTask.CompletedTask;
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? CapturedContext { get; private set; }
        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            CapturedContext = context;
            return ValueTask.FromResult(true);
        }
        public ValueTask WriteAsync(ProblemDetailsContext context) => ValueTask.CompletedTask;
    }
}
