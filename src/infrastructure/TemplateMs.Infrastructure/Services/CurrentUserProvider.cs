using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TemplateMs.Application.Common.Interfaces;

namespace TemplateMs.Infrastructure.Services;

/// <summary>
/// Reads the current user identity from the JWT token via <see cref="IHttpContextAccessor"/>.
/// Falls back to a newly generated <see cref="Guid"/> when:
/// <list type="bullet">
///   <item>there is no active HTTP context (background/messaging operations), or</item>
///   <item>the principal carries no <c>sub</c> / <c>NameIdentifier</c> claim, or</item>
///   <item>the claim value cannot be parsed as a <see cref="Guid"/>.</item>
/// </list>
/// </summary>
public sealed class CurrentUserProvider : ICurrentUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new <see cref="CurrentUserProvider"/>.</summary>
    public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public Guid UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal is null)
                return Guid.NewGuid();

            // ASP.NET Core maps the JWT standard "sub" claim to NameIdentifier.
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirst("sub");

            return claim is not null && Guid.TryParse(claim.Value, out var userId)
                ? userId
                : Guid.NewGuid();
        }
    }
}
