using System.Net.Http.Json;
using Flights.Api.Database;
using Flights.Api.Domain.Seats;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using TechTalk.SpecFlow;

namespace Flights.Api.AcceptanceTests.StepDefinitions;

[Binding]
internal sealed class BookSeatForFlightSteps : IDisposable
{
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;
    public BookSeatForFlightSteps(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [When("seat (.*) in (.*) class on flight (.*) is booked for passenger (.*) with email address (.*)")]
    public async Task WhenSeatInClassOnFlightIsBookedForPassengerWithEmailAddress(string seatNumber, string seatType, string flightNumber, string name, string email)
    {
        var flight = await _dbContext.Flights
                                     .SingleAsync(f => f.FlightNumber == flightNumber);
        var seat = flight.Seats.Single(s => s.SeatNumber == seatNumber);
        if (!Enum.TryParse<SeatType>(seatType, true, out var type))
        {
            throw new InvalidOperationException($"Invalid seat type: {seatType}");
        }
        if (seat.SeatType != type)
        {
            throw new InvalidOperationException($"Seat {seatNumber} is not of type {seatType}");
        }
        var request = new BookSeatRequest(seat.Id, name, email);
        var uri = new Uri($"/bookings", UriKind.Relative);
        _response = await _client.PostAsJsonAsync(uri, request);
    }

    [Then("seat (.*) in (.*) class on flight (.*) is booked for passenger (.*) with email address (.*) at a price of (.*)")]
    public async Task ThenSeatInClassOnFlightIsBookedForPassengerWithEmailAddressAtAPriceOf(string seatNumber, string seatType, string flightNumber, string name, string email, decimal price)
    {
        ArgumentNullException.ThrowIfNull(_response);
        _response.EnsureSuccessStatusCode();
        var booking = await _response.Content.ReadFromJsonAsync<BookingDto>();
        ArgumentNullException.ThrowIfNull(booking);
        booking.PassengerName.Should().Be(name);
        booking.PassengerEmail.Should().Be(email);
        booking.SeatNumber.Should().Be(seatNumber);
        booking.SeatType.Should().Be(seatType);
        booking.Price.Should().Be(price);
        booking.FlightNumber.Should().Be(flightNumber);
    }
    public void Dispose()
    {
        _response?.Dispose();
        _scope.Dispose();
        _client.Dispose();
        _dbContext.Dispose();
    }
}
