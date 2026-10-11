namespace TemplateMs.Application.Exceptions;

/// <summary>
/// Thrown by the validation pipeline when one or more FluentValidation rules fail.
/// Maps to HTTP 400 Bad Request in the error handling middleware.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>Gets the collection of validation failure messages grouped by property name.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>Initializes a new <see cref="ValidationException"/> from a set of FluentValidation failures.</summary>
    /// <param name="failures">The validation failures to capture.</param>
    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : base("One or more validation failures occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.Ordinal);
    }
}
