using System.Net;
using System.Text.Json;
using Flights.Api.FunctionalTests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Flights.Api.FunctionalTests;

public abstract class BaseFunctionalTest : IClassFixture<FunctionalTestWebAppFactory>
{
    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory)
    {
        JsonSerializerOptions = factory.Services.GetRequiredService<JsonSerializerOptions>();
        HttpClient = factory.CreateClient();
        LongString = new string('A', 101);
    }
    protected JsonSerializerOptions JsonSerializerOptions { get; }
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
        var actualProblemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(content, JsonSerializerOptions);
        ArgumentNullException.ThrowIfNull(actualProblemDetails);
        actualProblemDetails.Should()
                            .BeEquivalentTo(expectedProblemDetails, options => options.Excluding(p => p.Extensions));
    }
}
