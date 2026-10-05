using Microsoft.Extensions.Logging;

namespace NotificationService.UnitTests;

public sealed record LogEntry(string Message, IReadOnlyList<KeyValuePair<string, object?>> Properties);

public sealed class CollectingLogger : ILogger
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToList()
            : [];

        Entries.Add(new LogEntry(formatter(state, exception), properties));
    }
}

public sealed class CollectingLogger<T>(CollectingLogger inner) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        inner.Log(logLevel, eventId, state, exception, formatter);
}

public static class LogEntryExtensions
{
    public static object? Property(this LogEntry entry, string name) =>
        entry.Properties.Single(property => property.Key == name).Value;
}
