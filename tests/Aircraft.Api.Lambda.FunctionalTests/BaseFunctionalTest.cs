using System.Net;
using System.Text.Json;
using Aircraft.Api.Lambda.FunctionalTests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Aircraft.Api.Lambda.FunctionalTests;

public abstract class BaseFunctionalTest : IClassFixture<FunctionalTestWebAppFactory>
{
    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory) => HttpClient = factory.CreateClient();
    protected HttpClient HttpClient { get; }
    protected static async Task GetProblemDetailsFromResponseAndAssert(HttpResponseMessage response, string detail)
    {
        var expectedProblemDetails = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => new ProblemDetails().WithValidationError(detail),
            HttpStatusCode.NotFound => new ProblemDetails().WithQueryError(detail),
            HttpStatusCode.Conflict => new ProblemDetails().WithConflictError(detail),
            _ => throw new InvalidOperationException()
        };
        var content = await response.Content
                                    .ReadAsStreamAsync();
        var actualProblemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(content);
        ArgumentNullException.ThrowIfNull(actualProblemDetails);
        actualProblemDetails.Should()
                            .BeEquivalentTo(expectedProblemDetails, options => options.Excluding(p => p.Extensions));
    }
}
