using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Shared.Contracts;

namespace Airports.Api.Lambda.FunctionalTests;

public class ListAirportsTests : BaseFunctionalTest
{
    public ListAirportsTests(FunctionalTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Should_ReturnOk_WhenListIsEmpty()
    {
        // Arrange
        await EnsureDynamoDbTableCreatedAsync();

        // Act
        var uri = new Uri("airports", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AirportDto>>();
        list.Should().BeEmpty();
    }
}
