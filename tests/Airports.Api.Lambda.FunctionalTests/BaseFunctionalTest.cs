using FluentAssertions;
using Shared.Extensions;

namespace Airports.Api.Lambda.FunctionalTests;

public abstract class BaseFunctionalTest : IClassFixture<FunctionalTestWebAppFactory>
{
    protected BaseFunctionalTest(FunctionalTestWebAppFactory factory)
    {
        HttpClient = factory.CreateClient();
        LongString = new string('A', 101);
    }
    protected HttpClient HttpClient { get; }
    protected string LongString { get; }
    protected static async Task GetProblemDetailsFromResponseAndAssert(HttpResponseMessage response, string detail)
    {
        var expected = response.StatusCode.CreateExpectedProblemDetails(detail);
        var actual = await response.ReadProblemDetailsAsync();
        actual.Should().BeEquivalentTo(expected, options => options.Excluding(p => p.Extensions));
    }
}
