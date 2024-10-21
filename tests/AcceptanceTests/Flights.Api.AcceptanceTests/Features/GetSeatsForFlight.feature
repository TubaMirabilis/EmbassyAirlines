Feature: Get Seats for a Flight
    As a customer
    I want to retrieve the seats for a flight
    So that I can select a seat when booking

    Scenario: Get seats for a flight without a seat type filter
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       |
        When I get the seats for flight EA123
        Then the following seat groups are returned:
            | SeatType | Count | Price | Available |
            | Economy  | 301   | 1200  | 301       |
            | Business | 36    | 4500  | 36        |

    Scenario: Get seats for a flight filtered by seat type
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       |
        When I get the economy seats for flight EA123
        Then the following seat groups are returned:
            | SeatType | Count | Price | Available |
            | Economy  | 301   | 1200  | 301       |
