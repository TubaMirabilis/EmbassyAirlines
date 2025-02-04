using System.Net.Http.Json;
using System.Text.Json;
using Flights.Api.FunctionalTests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

namespace Flights.Api.FunctionalTests.Abstractions;

public abstract class BaseFunctionalTest : IClassFixture<FunctionalTestWebAppFactory>
{
    private readonly JsonSerializerOptions _options;
    private readonly IServiceScope _scope;
    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        _options = _scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>();
        HttpClient = factory.CreateClient();
        LongString = new string('A', 101);
    }
    protected HttpClient HttpClient { get; set; }
    protected string LongString { get; }
    protected async Task GetProblemDetailsFromResponseAndAssert(HttpResponseMessage response, string detail)
    {
        var expectedProblemDetails = new ProblemDetails().WithValidationError(detail);
        var content = await response.Content
                                    .ReadAsStreamAsync();
        var actualProblemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(content, _options);
        ArgumentNullException.ThrowIfNull(actualProblemDetails);
        actualProblemDetails.Should()
                            .BeEquivalentTo(expectedProblemDetails, options => options.Excluding(p => p.Extensions));
    }
    protected async Task<AirportDto> SeedAirportAsync(CreateAirportDto request)
    {
        var response = await HttpClient.PostAsJsonAsync("airports", request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content
                                    .ReadAsStreamAsync();
        var dto = await JsonSerializer.DeserializeAsync<AirportDto>(content, _options) ?? throw new JsonException();
        return dto;
    }
    public void Dispose() => _scope.Dispose();
}
