using FluentAssertions;
using TemplateMs.Application.Common.Interfaces;
using TemplateMs.Infrastructure.Database;
using Xunit;

namespace TemplateMs.Infrastructure.Tests;

/// <summary>Unit tests for the connection factory implementations.</summary>
public sealed class ConnectionFactoryTests
{
    [Fact]
    public void CommandDbConnectionFactoryWhenConnectionStringIsSetCanBeInstantiated()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(
            new Configuration.InfrastructureOptions
            {
                CommandConnectionString = "Server=.;Database=Test;Integrated Security=true;TrustServerCertificate=true;",
                QueryConnectionString = "Server=.;Database=Test;Integrated Security=true;TrustServerCertificate=true;"
            });

        // Act
        var factory = new CommandDbConnectionFactory(options);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void QueryDbConnectionFactoryWhenConnectionStringIsSetCanBeInstantiated()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(
            new Configuration.InfrastructureOptions
            {
                CommandConnectionString = "Server=.;Database=Test;Integrated Security=true;TrustServerCertificate=true;",
                QueryConnectionString = "Server=.;Database=Test;Integrated Security=true;TrustServerCertificate=true;"
            });

        // Act
        var factory = new QueryDbConnectionFactory(options);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void CommandDbConnectionFactoryImplementsICommandDbConnectionFactory()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(
            new Configuration.InfrastructureOptions { CommandConnectionString = "Server=." });

        // Act
        var factory = new CommandDbConnectionFactory(options);

        // Assert
        factory.Should().BeAssignableTo<ICommandDbConnectionFactory>();
    }

    [Fact]
    public void QueryDbConnectionFactoryImplementsIQueryDbConnectionFactory()
    {
        // Arrange
        var options = Microsoft.Extensions.Options.Options.Create(
            new Configuration.InfrastructureOptions { QueryConnectionString = "Server=." });

        // Act
        var factory = new QueryDbConnectionFactory(options);

        // Assert
        factory.Should().BeAssignableTo<IQueryDbConnectionFactory>();
    }
}
