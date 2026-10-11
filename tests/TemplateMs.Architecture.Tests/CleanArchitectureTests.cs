using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace TemplateMs.Architecture.Tests;

/// <summary>
/// Enforces Clean Architecture dependency rules:
///   Domain → (nothing)
///   Application → Domain only
///   Infrastructure → Domain + Application  (never Api)
///   Api → Application + Domain             (never Infrastructure internals)
///   Sql → (nothing outside itself)
///
/// The dependency inversion principle is validated by confirming that inner rings
/// carry no compile-time references to outer rings.
/// </summary>
public sealed class CleanArchitectureTests
{
    // Domain ring 

    [Fact]
    public void DomainDoesNotDependOnApplication()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain is the innermost ring and must not reference Application");
    }

    [Fact]
    public void DomainDoesNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not reference Infrastructure — implementations flow inward via DI");
    }

    [Fact]
    public void DomainDoesNotDependOnApi()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not reference the presentation layer");
    }

    [Fact]
    public void DomainDoesNotDependOnSql()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Sql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not reference SQL string constants — that is an infrastructure concern");
    }

    // Application ring 

    [Fact]
    public void ApplicationDoesNotDependOnInfrastructure()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application defines interfaces; Infrastructure provides the implementations — not the reverse");
    }

    [Fact]
    public void ApplicationDoesNotDependOnApi()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not know about the HTTP presentation layer");
    }

    [Fact]
    public void ApplicationDoesNotDependOnSql()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Sql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not reference raw SQL string constants");
    }

    // Infrastructure ring 

    [Fact]
    public void InfrastructureDoesNotDependOnApi()
    {
        var result = Types.InAssembly(AssemblyReferences.Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure must not reference the presentation layer");
    }

    // Api ring ─

    [Fact]
    public void ApiDoesNotDependOnInfrastructureInternals()
    {
        // The Api layer is allowed to reference Infrastructure only through the DI
        // composition root (AddInfrastructureServices). It must never directly reference
        // concrete Infrastructure types from controllers or middleware.
        var result = Types.InAssembly(AssemblyReferences.Api)
            .That()
            .ResideInNamespace("TemplateMs.Api.Controllers")
            .Or()
            .ResideInNamespace("TemplateMs.Api.Middleware")
            .Or()
            .ResideInNamespace("TemplateMs.Api.Filters")
            .Or()
            .ResideInNamespace("TemplateMs.Api.ResponseModels")
            .Or()
            .ResideInNamespace("TemplateMs.Api.RequestModels")
            .ShouldNot()
            .HaveDependencyOn("TemplateMs.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Controllers, middleware and response models must depend on Application abstractions only");
    }

    // Sql ring 

    [Fact]
    public void SqlDoesNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(AssemblyReferences.Sql)
            .ShouldNot()
            .HaveDependencyOnAny(
                "TemplateMs.Domain",
                "TemplateMs.Application",
                "TemplateMs.Infrastructure",
                "TemplateMs.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "The Sql project contains only raw SQL string constants and must carry no layer dependencies");
    }

    // Dependency inversion 

    [Fact]
    public void InfrastructureRepositoriesImplementApplicationInterfaces()
    {
        // Confirm the DIP: Infrastructure classes that end in "Repository" implement
        // an interface defined in the Application layer.
        var repositories = Types.InAssembly(AssemblyReferences.Infrastructure)
            .That()
            .HaveNameEndingWith("Repository")
            .GetTypes();

        foreach (var repo in repositories)
        {
            var implementsAppInterface = repo.GetInterfaces()
                .Any(i => i.Namespace?.StartsWith("TemplateMs.Application") == true);

            implementsAppInterface.Should().BeTrue(
                $"{repo.FullName} is a repository but does not implement any Application interface");
        }
    }
}
