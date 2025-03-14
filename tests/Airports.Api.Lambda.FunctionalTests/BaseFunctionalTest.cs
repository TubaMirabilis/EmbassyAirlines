using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Airports.Api.Lambda.FunctionalTests.Extensions;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

namespace Airports.Api.Lambda.FunctionalTests;

public abstract class BaseFunctionalTest : IClassFixture<FunctionalTestWebAppFactory>
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly JsonSerializerOptions _options;
    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        _options = scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        _dynamoDbClient = factory.Services.GetRequiredService<IAmazonDynamoDB>();
        HttpClient = factory.CreateClient();
        LongString = new string('A', 101);
    }
    protected HttpClient HttpClient { get; }
    protected string LongString { get; }
    protected async Task GetProblemDetailsFromResponseAndAssert(HttpResponseMessage response, string detail)
    {
        var expectedProblemDetails = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ProblemDetails().WithValidationError(detail),
            HttpStatusCode.NotFound => new ProblemDetails().WithQueryError(detail),
            _ => throw new InvalidOperationException()
        };
        var content = await response.Content
                                    .ReadAsStreamAsync();
        var actualProblemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(content, _options);
        ArgumentNullException.ThrowIfNull(actualProblemDetails);
        actualProblemDetails.Should()
                            .BeEquivalentTo(expectedProblemDetails, options => options.Excluding(p => p.Extensions));
    }
    protected async Task<AirportDto> SeedAirportAsync(CreateOrUpdateAirportDto request)
    {
        await EnsureDynamoDbTableCreatedAsync();
        var response = await HttpClient.PostAsJsonAsync("airports", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content
                                    .ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<AirportDto>(content, _options) ?? throw new JsonException();
        return dto;
    }
    protected async Task EnsureDynamoDbTableCreatedAsync()
    {
        var tables = await _dynamoDbClient.ListTablesAsync();
        if (!tables.TableNames.Contains("airports"))
        {
            await _dynamoDbClient.CreateTableAsync(new CreateTableRequest
            {
                TableName = "airports",
                AttributeDefinitions =
            [
                new("Id", ScalarAttributeType.S)
            ],
                KeySchema =
            [
                new("Id", KeyType.HASH)
            ],
                ProvisionedThroughput = new ProvisionedThroughput(1, 1)
            });
        }
    }
}
