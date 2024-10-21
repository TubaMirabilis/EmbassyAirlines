using Flights.Api.AcceptanceTests.Extensions;
using Flights.Api.Database;
using Flights.Api.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TechTalk.SpecFlow;

namespace Flights.Api.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class CommonSteps : IDisposable
{
    private readonly IServiceScope _scope;
    public CommonSteps(WebApplicationFactory<Program> factory)
    {
        _scope = factory.Services.CreateScope();
    }
    public void Dispose()
    {
        _scope.Dispose();
    }
    [Given(@"the following flights exist:")]
    public async Task GivenTheFollowingFlightsExist(Table table)
    {
        List<Flight> flights = [];
        foreach (var row in table.Rows)
        {
            flights.Add(row.ParseFlight());
        }
        using var dbContext = _scope.ServiceProvider
                                    .GetRequiredService<ApplicationDbContext>();
        dbContext.Flights
                 .AddRange(flights);
        await dbContext.SaveChangesAsync();
    }
}
