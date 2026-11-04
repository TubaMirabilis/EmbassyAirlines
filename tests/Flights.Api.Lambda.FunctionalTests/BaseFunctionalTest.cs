using AwesomeAssertions;
using Shared.HttpApiLambdaDefaults.Extensions;

namespace Flights.Api.Lambda.FunctionalTests;

public abstract class BaseFunctionalTest : IClassFixture<FunctionalTestWebAppFactory>
{
    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory) => HttpClient = factory.CreateClient();
    protected HttpClient HttpClient { get; }
    protected static async Task GetProblemDetailsFromResponseAndAssert(HttpResponseMessage response, string detail)
    {
        var expected = response.StatusCode.CreateExpectedProblemDetails(detail);
        var actual = await response.ReadProblemDetailsAsync();
        actual.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.Extensions));
    }
    protected static async Task GetProblemDetailsFromResponseAndAssertDetailStartsWith(
    HttpResponseMessage response,
    string expectedDetailPrefix)
    {
        var expected = response.StatusCode.CreateExpectedProblemDetails(
            expectedDetailPrefix);

        var actual = await response.ReadProblemDetailsAsync();

        actual.Should().BeEquivalentTo(
            expected,
            options => options
                .Excluding(p => p.Extensions)
                .Excluding(p => p.Detail));

        actual.Detail.Should().StartWith(expectedDetailPrefix);
    }
}
