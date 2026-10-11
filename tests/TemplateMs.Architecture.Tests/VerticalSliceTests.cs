using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace TemplateMs.Architecture.Tests;

/// <summary>
/// Enforces Vertical Slice Architecture rules inside the Application layer:
///
///   • Commands, queries, handlers, validators, DTOs, events, and interfaces
///     for a given feature (e.g., Order) are co-located under
///     Features/{Feature}/* — not scattered across horizontal layers.
///
///   • Handler types live in the same namespace as their command/query.
///
///   • Cross-slice: types in one feature's namespace must not directly reference
///     another feature's namespace (slices communicate via integration events,
///     not direct type coupling).
///
///   • Common/ and Behaviors/ are the only namespaces that are legitimately
///     horizontal within the Application layer.
/// </summary>
public sealed class VerticalSliceTests
{
    private static readonly string[] AllowedHorizontalNamespaces =
    [
        "TemplateMs.Application.Common",
        "TemplateMs.Application.Behaviors",
        "TemplateMs.Application.Exceptions",
        "TemplateMs.Application.Validation",
        "TemplateMs.Application.Events",   // integration events are cross-cutting by design
    ];

    // Co-location rules 

    [Fact]
    public void CommandHandlersResideInSameNamespaceAsTheirCommand()
    {
        var handlers = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .GetTypes();

        foreach (var handler in handlers)
        {
            var commandName = handler.Name.Replace("Handler", string.Empty);
            var commandType = AssemblyReferences.Application.GetType(
                $"{handler.Namespace}.{commandName}");

            commandType.Should().NotBeNull(
                $"{handler.FullName} must be co-located with {commandName} in the same namespace");
        }
    }

    [Fact]
    public void QueryHandlersResideInSameNamespaceAsTheirQuery()
    {
        var handlers = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .GetTypes();

        foreach (var handler in handlers)
        {
            var queryName = handler.Name.Replace("Handler", string.Empty);
            var queryType = AssemblyReferences.Application.GetType(
                $"{handler.Namespace}.{queryName}");

            queryType.Should().NotBeNull(
                $"{handler.FullName} must be co-located with {queryName} in the same namespace");
        }
    }

    [Fact]
    public void ValidatorsResideInSameNamespaceAsTheirCommand()
    {
        var validators = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Validator")
            .And()
            .ResideInNamespaceContaining("Features")
            .GetTypes();

        foreach (var validator in validators)
        {
            var commandName = validator.Name.Replace("Validator", string.Empty);
            var commandType = AssemblyReferences.Application.GetType(
                $"{validator.Namespace}.{commandName}");

            commandType.Should().NotBeNull(
                $"{validator.FullName} must be co-located with {commandName} in the same namespace");
        }
    }

    [Fact]
    public void DomainEventHandlersResideUnderFeaturesEventsNamespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("DomainEventHandler")
            .Should()
            .ResideInNamespaceContaining("Features")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain event handlers belong to the feature slice that owns the event, not a horizontal layer");
    }

    [Fact]
    public void FeatureDtosResideUnderFeaturesNamespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Dto")
            .Should()
            .ResideInNamespaceContaining("Features")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "DTOs are feature-specific and must live inside their feature slice under Features/");
    }

    [Fact]
    public void FeatureInterfacesResideUnderFeaturesInterfacesNamespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameStartingWith("IOrder")
            .Should()
            .ResideInNamespaceContaining("Features")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Repository and service interfaces that are feature-specific must reside in the feature slice");
    }

    // Cross-slice isolation                       

    [Fact]
    public void FeatureSlicesDoNotDirectlyCrossReference()
    {
        // Discover all distinct feature slice namespaces (e.g., Features.Order, Features.Invoice …)
        var featureNamespaces = AssemblyReferences.Application
            .GetTypes()
            .Select(t => t.Namespace ?? string.Empty)
            .Where(ns => ns.Contains(".Features."))
            .Select(ns =>
            {
                // Extract "TemplateMs.Application.Features.{Slice}" prefix
                var parts = ns.Split('.');
                var featuresIndex = Array.IndexOf(parts, "Features");
                return featuresIndex >= 0 && featuresIndex + 1 < parts.Length
                    ? string.Join(".", parts[..(featuresIndex + 2)])
                    : null;
            })
            .Where(ns => ns is not null)
            .Distinct()
            .ToList();

        // With only one slice there are no cross-slice dependencies by definition.
        if (featureNamespaces.Count < 2)
            return;

        foreach (var slice in featureNamespaces)
        {
            var otherSlices = featureNamespaces
                .Where(ns => ns != slice)
                .ToArray();

            var result = Types.InAssembly(AssemblyReferences.Application)
                .That()
                .ResideInNamespaceContaining(slice!)
                .ShouldNot()
                .HaveDependencyOnAny(otherSlices!)
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Feature slice '{slice}' must not directly depend on other slices: {string.Join(", ", otherSlices!)}. " +
                "Cross-slice communication must go through integration events.");
        }
    }

    // Horizontal layer guard                       

    [Fact]
    public void AllApplicationTypesResideEitherInFeatureSlicesOrApprovedHorizontalNamespaces()
    {
        var allApplicationTypes = AssemblyReferences.Application
            .GetTypes()
            .Where(t => t.IsPublic && !t.IsNested)
            .ToList();

        var violations = allApplicationTypes
            .Where(t =>
            {
                var ns = t.Namespace ?? string.Empty;
                var inFeature = ns.Contains(".Features.");
                var inHorizontal = AllowedHorizontalNamespaces.Any(h => ns.StartsWith(h));
                return !inFeature && !inHorizontal;
            })
            .ToList();

        violations.Should().BeEmpty(
            "Every public Application type must live in a feature slice (Features/) or an " +
            "approved horizontal namespace (Common/, Behaviors/, Exceptions/, Validation/, Events/)");
    }
}
