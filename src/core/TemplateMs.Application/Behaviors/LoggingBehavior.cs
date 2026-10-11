using Microsoft.Extensions.Logging;
using Wolverine;

namespace TemplateMs.Application.Behaviors;

/// <summary>
/// Wolverine middleware that logs the start and completion (or failure) of every message handler invocation.
/// Registered globally via <c>WolverineRegistration</c>.
/// </summary>
public sealed class LoggingBehavior
{
    private static class LogMessages
    {
        public const string HandlingMessage = "Handling message {MessageType} with id {MessageId}";
        public const string MessageHandled = "Successfully handled message {MessageType} with id {MessageId}";
        public const string MessageFailed = "Failed to handle message {MessageType} with id {MessageId}";
    }

    /// <summary>Executes before the handler. Logs the incoming message type.</summary>
    /// <param name="envelope">The Wolverine envelope containing message metadata.</param>
    /// <param name="logger">Injected logger.</param>
    public static void Before(Envelope envelope, ILogger<LoggingBehavior> logger) =>
        logger.LogInformation(LogMessages.HandlingMessage, envelope.MessageType, envelope.Id);

    /// <summary>Executes after a successful handler. Logs completion.</summary>
    /// <param name="envelope">The Wolverine envelope containing message metadata.</param>
    /// <param name="logger">Injected logger.</param>
    public static void After(Envelope envelope, ILogger<LoggingBehavior> logger) =>
        logger.LogInformation(LogMessages.MessageHandled, envelope.MessageType, envelope.Id);

    /// <summary>Executes when the handler throws. Logs the failure.</summary>
    /// <param name="envelope">The Wolverine envelope containing message metadata.</param>
    /// <param name="logger">Injected logger.</param>
    public static void Finally(Envelope envelope, ILogger logger)
    {
        // Note: Do not log exceptions here; use OnException for errors
        logger.LogInformation("Finished processing envelope {Id}", envelope.Id);
    }

    /// <summary>Executes when an exception occurs during handler execution. Logs the exception details and rethrows.</summary>
    /// <param name="ex">The exception that was thrown.</param>
    /// <param name="envelope">The Wolverine envelope containing message metadata.</param>
    /// <param name="logger">Injected logger.</param>
    /// <exception cref="Exception">Always rethrows <paramref name="ex"/> so callers (e.g. IMessageBus.InvokeAsync) observe the failure.</exception>
    public static void OnException(Exception ex, Envelope envelope, ILogger logger)
    {
        // This runs only when an exception is thrown. Rethrow so the original exception
        // propagates to the caller instead of being swallowed by Wolverine's compensating frame.
        logger.LogError(ex, LogMessages.MessageFailed, envelope.MessageType, envelope.Id);
        throw ex;
    }
}
