using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Wookashi.FeatureSwitcher.Shared.Abstraction.Logger;

/// <summary>
/// Single-line console format: <c>level: Category: message</c>. Exceptions are written below the
/// message — as a type/message chain (including inner exceptions) for warnings and below, and with the
/// full stack trace for errors and critical entries.
/// </summary>
public sealed class MinimalConsoleFormatter() : ConsoleFormatter("minimal")
{
    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        if (string.IsNullOrWhiteSpace(message))
            return;

        var level = logEntry.LogLevel switch
        {
            LogLevel.Trace => "trace",
            LogLevel.Debug => "debug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warning",
            LogLevel.Error => "error",
            LogLevel.Critical => "critical",
            _ => "none",
        };

        textWriter.Write(level);
        textWriter.Write(": ");
        textWriter.Write(ShortCategory(logEntry.Category));
        textWriter.Write(": ");
        textWriter.WriteLine(message);

        if (logEntry.Exception is not null)
        {
            WriteException(logEntry.Exception, logEntry.LogLevel, message, textWriter);
        }
    }

    private static string ShortCategory(string category)
    {
        var lastDot = category.LastIndexOf('.');
        return lastDot < 0 ? category : category[(lastDot + 1)..];
    }

    private static void WriteException(Exception exception, LogLevel level, string message, TextWriter textWriter)
    {
        if (level >= LogLevel.Error)
        {
            var details = exception.ToString();
            // Some loggers (e.g. EF Core) already embed the full exception text in the message.
            if (!message.Contains(details, StringComparison.Ordinal))
            {
                textWriter.WriteLine(details);
            }
            return;
        }

        textWriter.Write(exception.GetType().Name);
        textWriter.Write(": ");
        textWriter.WriteLine(exception.Message);

        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            textWriter.Write(" ---> ");
            textWriter.Write(inner.GetType().Name);
            textWriter.Write(": ");
            textWriter.WriteLine(inner.Message);
        }
    }
}
