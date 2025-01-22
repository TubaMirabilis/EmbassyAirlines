Feature: Book seat for flight
    As a user
    I want to book a seat for a flight
    So that the seat is reserved for the passenger

    Scenario: Book a seat for a flight
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2026-01-01T10:00:00 | 2026-01-02T05:00:00 | 1200.00      | 4500.00       |
        When seat 1A in Business class on flight EA123 is booked for passenger John Doe with email address john.doe@aol.com
        Then seat 1A in Business class on flight EA123 is booked for passenger John Doe with email address john.doe@aol.com at a price of 4500.00