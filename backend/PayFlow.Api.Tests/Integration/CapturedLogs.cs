using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace PayFlow.Api.Tests.Integration;

internal sealed class CapturedLogs : ILoggerProvider
{
    private readonly ConcurrentQueue<string> messages = new();
    public IReadOnlyList<string> Messages => messages.ToArray();
    public ILogger CreateLogger(string categoryName) => new Logger(messages);
    public void Dispose() { }

    private sealed class Logger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception) + exception);
    }
}
