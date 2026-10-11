using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;
using TemplateMs.Domain.Common;
using TemplateMs.Domain.Exceptions;
using Xunit;

namespace TemplateMs.Architecture.Tests;

/// <summary>
/// Enforces domain model invariants that prevent architectural decay:
///
///   • Domain entities live in TemplateMs.Domain.Entities.
///   • Domain events live in TemplateMs.Domain.Events.
///   • Domain exceptions live in TemplateMs.Domain.Exceptions.
///   • All concrete entities extend BaseAuditableEntity (through AggregateRoot or directly).
///   • Aggregate roots extend AggregateRoot.
///   • Concrete entities and exceptions are sealed.
///   • Domain events are sealed records (not mutable classes).
///   • Domain exceptions extend DomainException.
///   • Entity business-state properties do not expose public setters
///     (Dapper audit fields are the only deliberate exception).
/// </summary>
public sealed class DomainModelTests
{
    // Audit properties that intentionally have public setters for Dapper hydration.
    private static readonly HashSet<string> AuditProperties =
    [
        "Id", "CreatedBy", "CreatedAt", "UpdatedBy", "UpdatedAt"
    ];

    // Namespace placement 

    [Fact]
    public void ConcreteEntitiesResideInDomainEntitiesNamespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .That()
            .Inherit(typeof(BaseAuditableEntity))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespace("TemplateMs.Domain.Entities")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Concrete domain entities must reside in TemplateMs.Domain.Entities");
    }

    [Fact]
    public void DomainEventsResideInDomainEventsNamespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .That()
            .Inherit(typeof(DomainEvent))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespace("TemplateMs.Domain.Events")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Concrete domain events must reside in TemplateMs.Domain.Events");
    }

    [Fact]
    public void DomainExceptionsResideInDomainExceptionsNamespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .That()
            .Inherit(typeof(DomainException))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespace("TemplateMs.Domain.Exceptions")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Concrete domain exceptions must reside in TemplateMs.Domain.Exceptions");
    }

    // Inheritance hierarchy                       

    [Fact]
    public void AggregateRootsInheritFromAggregateRoot()
    {
        // Any class in Domain.Entities that has a DomainEvents property is an aggregate root.
        var aggregateCandidates = AssemblyReferences.Domain
            .GetTypes()
            .Where(t => t.Namespace == "TemplateMs.Domain.Entities"
                     && !t.IsAbstract
                     && t.IsClass);

        foreach (var type in aggregateCandidates)
        {
            var hasDomainEvents = type.GetProperty("DomainEvents") is not null;
            if (!hasDomainEvents) continue;

            type.IsSubclassOf(typeof(AggregateRoot)).Should().BeTrue(
                $"{type.FullName} exposes DomainEvents (aggregate root behaviour) " +
                "but does not extend AggregateRoot");
        }
    }

    [Fact]
    public void NonAggregateEntitiesInheritFromBaseAuditableEntity()
    {
        var nonAggregates = AssemblyReferences.Domain
            .GetTypes()
            .Where(t => t.Namespace == "TemplateMs.Domain.Entities"
                     && !t.IsAbstract
                     && t.IsClass
                     && !t.IsSubclassOf(typeof(AggregateRoot))
                     && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false));

        foreach (var type in nonAggregates)
        {
            type.IsSubclassOf(typeof(BaseAuditableEntity)).Should().BeTrue(
                $"{type.FullName} is a domain entity but does not extend BaseAuditableEntity");
        }
    }

    // Sealed constraints ─

    [Fact]
    public void ConcreteEntitiesAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .That()
            .ResideInNamespace("TemplateMs.Domain.Entities")
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Concrete domain entities must be sealed to prevent accidental inheritance");
    }

    [Fact]
    public void DomainExceptionsAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .That()
            .Inherit(typeof(DomainException))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Concrete domain exceptions must be sealed");
    }

    [Fact]
    public void DomainEventsAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .That()
            .ResideInNamespace("TemplateMs.Domain.Events")
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain events must be sealed records — they are immutable facts");
    }

    [Fact]
    public void DomainEventsAreRecords()
    {
        var events = AssemblyReferences.Domain
            .GetTypes()
            .Where(t => t.Namespace == "TemplateMs.Domain.Events" && !t.IsAbstract);

        foreach (var evt in events)
        {
            var isRecord = evt.GetMethod("<Clone>$") is not null;
            isRecord.Should().BeTrue(
                $"{evt.FullName} is a domain event but not a record. " +
                "Domain events are immutable facts and must be declared as records");
        }
    }

    // Immutability: no public setters on business properties      

    [Fact]
    public void DomainEntityBusinessPropertiesDoNotHavePublicSetters()
    {
        var entities = AssemblyReferences.Domain
            .GetTypes()
            .Where(t => t.Namespace == "TemplateMs.Domain.Entities"
                     && !t.IsAbstract
                     && t.IsClass);

        var violations = new List<string>();

        foreach (var entity in entities)
        {
            var properties = entity
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => !AuditProperties.Contains(p.Name))
                .Where(p => p.SetMethod is { IsPublic: true });

            violations.AddRange(properties.Select(
                p => $"{entity.Name}.{p.Name}"));
        }

        violations.Should().BeEmpty(
            "Domain entity business properties must not have public setters. " +
            "State changes must go through domain methods to enforce invariants. " +
            $"Audit fields ({string.Join(", ", AuditProperties)}) are exempt.");
    }

    // Domain layer purity 

    [Fact]
    public void DomainHasNoDependencyOnMicrosoftExtensions()
    {
        var result = Types.InAssembly(AssemblyReferences.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Configuration")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "The Domain layer must be framework-agnostic and must not reference Microsoft.Extensions.* packages");
    }
}
