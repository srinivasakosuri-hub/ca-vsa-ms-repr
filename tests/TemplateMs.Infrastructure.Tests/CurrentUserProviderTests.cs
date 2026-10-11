using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Infrastructure.Services;
using Xunit;

namespace TemplateMs.Infrastructure.Tests;

/// <summary>Unit tests for <see cref="CurrentUserProvider"/>.</summary>
public sealed class CurrentUserProviderTests
{
    [Fact]
    public void CurrentUserProviderImplementsICurrentUserProvider()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();

        // Act
        var provider = new CurrentUserProvider(accessor);

        // Assert
        provider.Should().BeAssignableTo<ICurrentUserProvider>();
    }

    [Fact]
    public void UserIdIsNonEmptyGuidWhenHttpContextIsNull()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var provider = new CurrentUserProvider(accessor);

        // Act
        var userId = provider.UserId;

        // Assert
        userId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UserIdReturnsGuidParsedFromSubClaim()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var claims = new[] { new Claim("sub", expected.ToString()) };
        var context = BuildHttpContext(claims);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        var provider = new CurrentUserProvider(accessor);

        // Act
        var userId = provider.UserId;

        // Assert
        userId.Should().Be(expected);
    }

    [Fact]
    public void UserIdReturnsGuidParsedFromNameIdentifierClaim()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, expected.ToString()) };
        var context = BuildHttpContext(claims);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        var provider = new CurrentUserProvider(accessor);

        // Act
        var userId = provider.UserId;

        // Assert
        userId.Should().Be(expected);
    }

    [Fact]
    public void UserIdPrefersNameIdentifierClaimOverSubClaim()
    {
        // Arrange
        var nameIdentifierId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, nameIdentifierId.ToString()),
            new Claim("sub", subId.ToString())
        };
        var context = BuildHttpContext(claims);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        var provider = new CurrentUserProvider(accessor);

        // Act
        var userId = provider.UserId;

        // Assert
        userId.Should().Be(nameIdentifierId);
    }

    [Fact]
    public void UserIdIsNonEmptyGuidWhenClaimValueIsNotAValidGuid()
    {
        // Arrange
        var claims = new[] { new Claim("sub", "not-a-valid-guid") };
        var context = BuildHttpContext(claims);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        var provider = new CurrentUserProvider(accessor);

        // Act
        var userId = provider.UserId;

        // Assert
        userId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UserIdIsNonEmptyGuidWhenPrincipalHasNoClaims()
    {
        // Arrange
        var context = BuildHttpContext([]);
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(context);
        var provider = new CurrentUserProvider(accessor);

        // Act
        var userId = provider.UserId;

        // Assert
        userId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UserIdReturnsDifferentGuidOnEachCallWhenNoHttpContext()
    {
        // Arrange — no context means fallback; each call should generate a fresh Guid
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var provider = new CurrentUserProvider(accessor);

        // Act
        var first = provider.UserId;
        var second = provider.UserId;

        // Assert
        first.Should().NotBe(Guid.Empty);
        second.Should().NotBe(Guid.Empty);
        first.Should().NotBe(second);
    }

    // -------------------------------------------------------------------------
    private static DefaultHttpContext BuildHttpContext(Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);
        return new DefaultHttpContext { User = principal };
    }
}
