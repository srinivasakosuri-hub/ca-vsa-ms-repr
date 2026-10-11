using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace TemplateMs.Architecture.Tests;

/// <summary>
/// Banned library guard rails. These tests ensure that prohibited packages
/// are never introduced into the production codebase.
///
///   BANNED EVERYWHERE
///        
///   • AutoMapper           — use manual mapping (OrderMapper) instead.
///   • MediatR              — use WolverineFX for command/query dispatch instead.
///   • System.ComponentModel.DataAnnotations (validation) — use FluentValidation instead.
///   • Entity Framework Core — use Dapper with the repository pattern instead.
///
///   BANNED FROM DOMAIN
///        
///   • Dapper               — Domain is persistence-ignorant.
///   • FluentValidation     — Domain encapsulates its own invariants via DomainException;
///                            FluentValidation lives in Application.
///
///   BANNED FROM APPLICATION
///            
///   • Dapper               — Application defines interfaces; SQL belongs in Infrastructure.
///   • System.Data.SqlClient / Microsoft.Data.SqlClient — Application must not
///                            reference a specific DB driver.
/// </summary>
public sealed class ForbiddenLibrariesTests
{
    // AutoMapper─

    [Fact]
    public void AutoMapperIsNotUsedAnywhere()
    {
        foreach (var assembly in AssemblyReferences.All)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny("AutoMapper", "AutoMapper.Extensions")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"AutoMapper is banned in {assembly.GetName().Name}. " +
                "Use manual mappers (e.g., OrderMapper) to keep mapping logic explicit and compile-time safe");
        }
    }

    // MediatR  

    [Fact]
    public void MediatRIsNotUsedAnywhere()
    {
        foreach (var assembly in AssemblyReferences.All)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny("MediatR", "MediatR.Extensions")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"MediatR is banned in {assembly.GetName().Name}. " +
                "Use WolverineFX (IMessageBus) for in-process command/query dispatch");
        }
    }

    [Fact]
    public void NoTypesImplementMediatRIRequestHandler()
    {
        // Defence-in-depth: check the interface name pattern in addition to the namespace check above.
        foreach (var assembly in AssemblyReferences.All)
        {
            var violators = assembly.GetTypes()
                .Where(t => t.GetInterfaces()
                    .Any(i => i.FullName?.StartsWith("MediatR.IRequestHandler") == true
                           || i.FullName?.StartsWith("MediatR.INotificationHandler") == true))
                .ToList();

            violators.Should().BeEmpty(
                $"Types in {assembly.GetName().Name} must not implement MediatR handler interfaces. " +
                "Use WolverineFX handler conventions (sealed class + Handle method) instead");
        }
    }

    // DataAnnotations (validation)                   ─

    [Fact]
    public void DataAnnotationsValidationAttributesAreNotUsedInDomain()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn("System.ComponentModel.DataAnnotations")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not use DataAnnotations for validation. " +
            "Domain invariants are enforced by throwing DomainException inside domain methods");
    }

    [Fact]
    public void DataAnnotationsValidationAttributesAreNotUsedInApplication()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOn("System.ComponentModel.DataAnnotations")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must use FluentValidation (AbstractValidator<T>), not DataAnnotations, " +
            "to keep validation rules explicit, testable, and co-located with the command");
    }

    [Fact]
    public void DataAnnotationsValidationAttributesAreNotUsedInInfrastructure()
    {
        var result = Types.InAssembly(AssemblyReferences.Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("System.ComponentModel.DataAnnotations")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Infrastructure must not use DataAnnotations for validation");
    }

    [Fact]
    public void DataAnnotationsValidationAttributesAreNotUsedOnApiRequestModels()
    {
        // Request models in the API layer must use FluentValidation in the Application layer,
        // not attribute-based validation on the DTO itself.
        var result = Types.InAssembly(AssemblyReferences.Api)
            .That()
            .ResideInNamespaceContaining("RequestModels")
            .ShouldNot()
            .HaveDependencyOn("System.ComponentModel.DataAnnotations")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "API request models must not carry DataAnnotations validation attributes. " +
            "Validation belongs in the Application layer as FluentValidation validators");
    }

    // Entity Framework Core                       

    [Fact]
    public void EntityFrameworkCoreIsNotUsedAnywhere()
    {
        foreach (var assembly in AssemblyReferences.All)
        {
            var result = Types.InAssembly(assembly)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.EntityFrameworkCore.Relational",
                    "Microsoft.EntityFrameworkCore.SqlServer")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Entity Framework Core is banned in {assembly.GetName().Name}. " +
                "Use Dapper with the repository pattern for data access");
        }
    }

    // Dapper banned from Domain and Application             

    [Fact]
    public void DapperIsNotUsedInDomain()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "The Domain layer is persistence-ignorant and must not reference Dapper");
    }

    [Fact]
    public void DapperIsNotUsedInApplication()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "The Application layer defines repository interfaces; Dapper is an implementation detail " +
            "that belongs exclusively in Infrastructure");
    }

    // FluentValidation banned from Domain                

    [Fact]
    public void FluentValidationIsNotUsedInDomain()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOn("FluentValidation")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain enforces its own invariants via DomainException. " +
            "FluentValidation belongs in the Application layer and must not appear in Domain");
    }

    // SQL client drivers banned from Application            ─

    [Fact]
    public void SqlClientDriversAreNotUsedInApplication()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not reference SQL client drivers directly. " +
            "Database access is abstracted via ICommandDbConnectionFactory / IQueryDbConnectionFactory");
    }

    [Fact]
    public void SqlClientDriversAreNotUsedInDomain()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must not reference SQL client drivers");
    }
}
