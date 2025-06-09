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
        await EnsureDynamoDbTableEmptyAsync();

        // Act
        var uri = new Uri("airports", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AirportDto>>(TestContext.Current.CancellationToken);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnOk_WhenListIsNotEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("LOWW", "VIE", "Vienna International Airport", "Europe/Vienna");
        await SeedAirportAsync(request);

        // Act
        var uri = new Uri("airports", UriKind.Relative);
        var response = await HttpClient.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<IEnumerable<AirportDto>>(TestContext.Current.CancellationToken);
        list.Should().HaveCount(1);
    }
}
