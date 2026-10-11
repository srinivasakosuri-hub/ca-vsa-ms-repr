using FluentAssertions;
using FluentValidation;
using NetArchTest.Rules;
using System.Reflection;
using Xunit;

namespace TemplateMs.Architecture.Tests;

/// <summary>
/// Enforces WolverineFX handler and messaging conventions:
///
///   Handlers
///   • Must be sealed (no inheritance chain for handlers).
///   • Must expose a public method named exactly "Handle".
///   • Must NOT implement IRequestHandler / ICommandHandler (MediatR patterns forbidden).
///
///   Commands and Queries
///   • Must be sealed records (immutable, value-typed messages).
///   • Command names end in "Command"; query names end in "Query".
///
///   DTOs
///   • Must be sealed records.
///
///   Integration Events
///   • Must be sealed records in the Application.Events namespace.
///
///   Validators
///   • Must extend AbstractValidator{T} from FluentValidation.
///   • Must be sealed.
///
///   Repository interfaces
///   • Read repositories start with "IOrder" and end with "ReadRepository".
///   • Write repositories start with "IOrder" and end with "WriteRepository".
/// </summary>
public sealed class HandlerConventionTests
{
    // Handler conventions 

    [Fact]
    public void CommandHandlersAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Handlers are leaf classes; sealing them prevents accidental subclassing");
    }

    [Fact]
    public void QueryHandlersAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Query handlers are leaf classes and must be sealed");
    }

    [Fact]
    public void CommandHandlersExposeAPublicHandleMethod()
    {
        var handlers = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("CommandHandler")
            .GetTypes();

        foreach (var handler in handlers)
        {
            var handleMethod = handler.GetMethod(
                "Handle",
                BindingFlags.Public | BindingFlags.Instance);

            handleMethod.Should().NotBeNull(
                $"{handler.FullName} must expose a public instance method named 'Handle' " +
                "so WolverineFX can discover it by convention");
        }
    }

    [Fact]
    public void QueryHandlersExposeAPublicHandleMethod()
    {
        var handlers = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("QueryHandler")
            .GetTypes();

        foreach (var handler in handlers)
        {
            var handleMethod = handler.GetMethod(
                "Handle",
                BindingFlags.Public | BindingFlags.Instance);

            handleMethod.Should().NotBeNull(
                $"{handler.FullName} must expose a public instance method named 'Handle' " +
                "so WolverineFX can discover it by convention");
        }
    }

    [Fact]
    public void DomainEventHandlersExposeAPublicHandleMethod()
    {
        var handlers = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("DomainEventHandler")
            .GetTypes();

        foreach (var handler in handlers)
        {
            var handleMethod = handler.GetMethod(
                "Handle",
                BindingFlags.Public | BindingFlags.Instance);

            handleMethod.Should().NotBeNull(
                $"{handler.FullName} must expose a public 'Handle' method for WolverineFX dispatch");
        }
    }

    [Fact]
    public void DomainEventHandlersAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("DomainEventHandler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain event handlers are leaf classes and must be sealed");
    }

    // Command / Query message conventions               ─

    [Fact]
    public void CommandsAreRecords()
    {
        // C# records emit a synthesised `<Clone>$` method.
        var commands = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .AreNotAbstract()
            .GetTypes()
            .Where(t => !t.Name.EndsWith("CommandHandler")
                     && !t.Name.EndsWith("CommandValidator"))
            .ToList();

        foreach (var command in commands)
        {
            var isRecord = command.GetMethod("<Clone>$") is not null;
            isRecord.Should().BeTrue(
                $"{command.FullName} looks like a command but is not a record. " +
                "Commands must be sealed records for immutability");
        }
    }

    [Fact]
    public void CommandsAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Command")
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Commands are value messages; they must be sealed to prevent inheritance hierarchies");
    }

    [Fact]
    public void QueriesAreRecords()
    {
        var queries = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreNotAbstract()
            .GetTypes()
            .Where(t => !t.Name.EndsWith("QueryHandler") && !t.Name.EndsWith("QueryValidator"));

        foreach (var query in queries)
        {
            var isRecord = query.GetMethod("<Clone>$") is not null;
            isRecord.Should().BeTrue(
                $"{query.FullName} looks like a query but is not a record. Queries must be sealed records");
        }
    }

    [Fact]
    public void QueriesAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Query")
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Queries must be sealed records");
    }

    // DTO conventions  

    [Fact]
    public void DtosAreRecords()
    {
        var dtos = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Dto")
            .GetTypes();

        foreach (var dto in dtos)
        {
            var isRecord = dto.GetMethod("<Clone>$") is not null;
            isRecord.Should().BeTrue(
                $"{dto.FullName} is a DTO but not a record. DTOs must be sealed records for immutability");
        }
    }

    [Fact]
    public void DtosAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Dto")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "DTOs are pure data carriers; sealing prevents unintended extension");
    }

    // Integration event conventions                   

    [Fact]
    public void IntegrationEventsAreRecords()
    {
        var events = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("IntegrationEvent")
            .GetTypes();

        foreach (var evt in events)
        {
            var isRecord = evt.GetMethod("<Clone>$") is not null;
            isRecord.Should().BeTrue(
                $"{evt.FullName} is an integration event but not a record. " +
                "Integration events must be records so they serialise deterministically");
        }
    }

    [Fact]
    public void IntegrationEventsAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("IntegrationEvent")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Integration events cross service boundaries and must be sealed records");
    }

    [Fact]
    public void IntegrationEventsResideInApplicationEventsNamespace()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("IntegrationEvent")
            .Should()
            .ResideInNamespace("TemplateMs.Application.Events")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Integration events are published across service boundaries and belong in the " +
            "shared Application.Events namespace, not inside a feature slice");
    }

    // Validator conventions                       

    [Fact]
    public void ValidatorsInheritFromAbstractValidatorOfT()
    {
        var validators = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Validator")
            .And()
            .ResideInNamespaceContaining("Features")
            .GetTypes();

        foreach (var validator in validators)
        {
            var inheritsAbstractValidator = validator.BaseType is { IsGenericType: true } bt
                && bt.GetGenericTypeDefinition() == typeof(AbstractValidator<>);

            inheritsAbstractValidator.Should().BeTrue(
                $"{validator.FullName} ends in 'Validator' but does not extend " +
                "FluentValidation.AbstractValidator<T>. Use FluentValidation — never DataAnnotations");
        }
    }

    [Fact]
    public void ValidatorsAreSealed()
    {
        var result = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .HaveNameEndingWith("Validator")
            .And()
            .ResideInNamespaceContaining("Features")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Validators are leaf classes; sealing them prevents unintended composition");
    }

    // Repository interface naming conventions              

    [Fact]
    public void ReadRepositoryInterfacesHaveCorrectNamingSuffix()
    {
        var readRepoInterfaces = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("ReadRepository")
            .GetTypes();

        // Ensure at least one exists — guards against the test silently vacuously passing
        readRepoInterfaces.Should().NotBeEmpty(
            "There must be at least one IXxxReadRepository interface in the Application layer");

        foreach (var iface in readRepoInterfaces)
        {
            iface.Name.Should().StartWith("I",
                $"{iface.Name} is a repository interface and must follow the 'IXxxReadRepository' naming convention");
        }
    }

    [Fact]
    public void WriteRepositoryInterfacesHaveCorrectNamingSuffix()
    {
        var writeRepoInterfaces = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("WriteRepository")
            .GetTypes();

        writeRepoInterfaces.Should().NotBeEmpty(
            "There must be at least one IXxxWriteRepository interface in the Application layer");

        foreach (var iface in writeRepoInterfaces)
        {
            iface.Name.Should().StartWith("I",
                $"{iface.Name} must follow the 'IXxxWriteRepository' naming convention");
        }
    }

    [Fact]
    public void ReadAndWriteRepositoriesAreSegregated()
    {
        // No single interface should serve as both read and write repository.
        var allRepoInterfaces = Types.InAssembly(AssemblyReferences.Application)
            .That()
            .AreInterfaces()
            .And()
            .HaveNameEndingWith("Repository")
            .GetTypes();

        foreach (var iface in allRepoInterfaces)
        {
            var isRead = iface.Name.EndsWith("ReadRepository");
            var isWrite = iface.Name.EndsWith("WriteRepository");

            (isRead || isWrite).Should().BeTrue(
                $"{iface.Name} must end with either 'ReadRepository' or 'WriteRepository' — " +
                "read and write sides must be clearly segregated (CQRS)");
        }
    }
}
