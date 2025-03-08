using System.Net;
using System.Net.Http.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Airports.Api.Lambda.FunctionalTests;

public class CreateAirportTests : BaseFunctionalTest
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    public CreateAirportTests(FunctionalTestWebAppFactory factory) : base(factory) => _dynamoDbClient = factory.Services.GetRequiredService<IAmazonDynamoDB>();

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto(LongString, "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenIataCodeIsInvalid()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("3/a", "Vancouver International Airport", "America/Vancouver");
        var error = "IATA Code must consist of 3 uppercase letters only.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("YVR", "", "America/Vancouver");
        var error = "Name is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenNameIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("YVR", LongString, "America/Vancouver");
        var error = "Name must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTimeZoneIdIsEmpty()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("YVR", "Vancouver International Airport", "");
        var error = "Time zone is required.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_WhenTimeZoneIdIsTooLong()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("YVR", "Vancouver International Airport", LongString);
        var error = "Time zone must not exceed 100 characters in length.";

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await GetProblemDetailsFromResponseAndAssert(response, error);
    }

    [Fact]
    public async Task Should_ReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        await _dynamoDbClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = "airports",
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition("Id", ScalarAttributeType.S)
            },
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement("Id", KeyType.HASH)
            },
            ProvisionedThroughput = new ProvisionedThroughput(1, 1)
        });
        var request = new CreateOrUpdateAirportDto("LHR", "London Heathrow Airport", "Europe/London");

        // Act
        var response = await SeedAirportAsync(request);

        // Assert
        response.Name.Should().Be(request.Name);
        response.IataCode.Should().Be(request.IataCode);
        response.TimeZoneId.Should().Be(request.TimeZoneId);
    }

    [Fact]
    public async Task Should_ReturnConflict_WhenAirportExists()
    {
        // Arrange
        var request = new CreateOrUpdateAirportDto("YVR", "Vancouver International Airport", "America/Vancouver");
        await SeedAirportAsync(request);

        // Act
        var response = await HttpClient.PostAsJsonAsync("airports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
