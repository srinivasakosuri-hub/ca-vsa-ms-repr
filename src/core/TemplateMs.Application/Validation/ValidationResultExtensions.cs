using FluentValidation.Results;

namespace TemplateMs.Application.Validation;

/// <summary>Extension methods for <see cref="ValidationResult"/> to integrate with the application exception model.</summary>
public static class ValidationResultExtensions
{
    /// <summary>
    /// Throws a <see cref="Exceptions.ValidationException"/> when the result contains failures.
    /// Call this at the boundary of any validator invocation.
    /// </summary>
    /// <param name="result">The validation result to inspect.</param>
    /// <exception cref="Exceptions.ValidationException">Thrown when <paramref name="result"/> is not valid.</exception>
    public static void ThrowIfInvalid(this ValidationResult result)
    {
        if (!result.IsValid)
            throw new Exceptions.ValidationException(result.Errors);
    }
}
