using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Shared.Npgsql.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDatabaseConnection_WithConfiguration_RegistersServices()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["DbConnection:Host"] = "localhost",
                    ["DbConnection:Port"] = "5432",
                    ["DbConnection:Database"] = "test_db",
                    ["DbConnection:Username"] = "test_user"
                })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddDatabaseConnection<TestDbContext>(
            configuration,
            useNodaTime: false,
            schema: "test_schema");

        // Assert
        using var provider = services.BuildServiceProvider();

        var dataSource = provider.GetService<NpgsqlDataSource>();

        dataSource.Should().NotBeNull();

        var dbContext = provider.GetService<TestDbContext>();

        dbContext.Should().NotBeNull();

        var interceptor =
            provider.GetService<InsertOutboxMessagesInterceptor>();

        interceptor.Should().NotBeNull();
    }
}
