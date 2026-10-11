namespace TemplateMs.Domain.Exceptions;

/// <summary>Base exception for resources that cannot be found. Maps to HTTP 404 Not Found.</summary>
public abstract class NotFoundException : DomainException
{
    /// <summary>Initializes a new <see cref="NotFoundException"/> with the specified message.</summary>
    /// <param name="message">Human-readable description of the missing resource.</param>
    protected NotFoundException(string message) : base(message) { }
}
