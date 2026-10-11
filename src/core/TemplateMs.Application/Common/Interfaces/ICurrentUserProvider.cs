namespace TemplateMs.Application.Common.Interfaces;

/// <summary>
/// Abstracts the identity of the currently authenticated user.
/// Returns the user's <see cref="Guid"/> parsed from the JWT <c>sub</c> / <c>NameIdentifier</c> claim,
/// or a newly generated <see cref="Guid"/> when no valid claim is present (e.g. system/background operations).
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// Gets the unique identifier of the current user.
    /// Guaranteed never to be <see cref="Guid.Empty"/>.
    /// </summary>
    Guid UserId { get; }
}
