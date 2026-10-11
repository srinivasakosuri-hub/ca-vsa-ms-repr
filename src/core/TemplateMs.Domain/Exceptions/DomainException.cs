namespace TemplateMs.Domain.Exceptions;

/// <summary>Base exception for domain rule violations. Thrown when a business invariant is breached.</summary>
public class DomainException : Exception
{
    /// <summary>Initializes a new <see cref="DomainException"/> with the specified message.</summary>
    /// <param name="message">Human-readable description of the domain rule violation.</param>
    public DomainException(string message) : base(message) { }

    /// <summary>Initializes a new <see cref="DomainException"/> with the specified message and inner exception.</summary>
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
