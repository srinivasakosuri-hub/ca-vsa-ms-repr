using FluentValidation;

namespace TemplateMs.Application.Behaviors;

/// <summary>
/// Wolverine middleware that runs FluentValidation validators before the handler executes.
/// Throws <see cref="Exceptions.ValidationException"/> on failure, which the error middleware maps to HTTP 400.
/// WolverineFx.FluentValidation handles automatic validator discovery; this class is a supplementary
/// explicit behavior for custom pipeline awareness.
/// </summary>
public sealed class ValidationBehavior
{
    /// <summary>
    /// Validates the message before the next middleware step.
    /// Delegates discovery and execution to the registered <see cref="IValidator{T}"/> for the message type.
    /// </summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="message">The incoming message.</param>
    /// <param name="validator">The optional FluentValidation validator (null-safe).</param>
    /// <param name="next">The next step in the Wolverine pipeline.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    public static async Task ExecuteAsync<T>(
        T message,
        IValidator<T>? validator,
        Func<Task> next,
        CancellationToken cancellationToken) where T : notnull
    {
        if (validator is not null)
        {
            var result = await validator.ValidateAsync(message, cancellationToken);
            result.ThrowIfInvalid();
        }

        await next();
    }
}

file static class ValidationResultExtensionsLocal
{
    public static void ThrowIfInvalid(this FluentValidation.Results.ValidationResult result)
    {
        if (!result.IsValid)
            throw new Exceptions.ValidationException(result.Errors);
    }
}
