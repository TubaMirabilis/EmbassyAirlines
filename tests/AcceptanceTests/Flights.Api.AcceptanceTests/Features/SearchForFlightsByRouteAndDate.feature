Feature: Search for flights by route and date
    As a customer
    I want to search for flights by route and date
    So that I can evaluate the options available to me

    Scenario: Search for flights by route and date with valid results
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       |
            | EA124        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T14:00:00 | 2025-01-02T09:00:00 | 1200.00      | 4500.00       |
            | EA125        | YVR                      | America/Vancouver        | LHR                        | Europe/London              | 2025-01-01T09:00:00 | 2025-01-02T04:00:00 | 1000.00      | 4000.00       |
        When I search for flights from YVR to CDG on 2025-01-01
        Then the following flights are returned:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | CheapestEconomyPrice | CheapestBusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00              | 4500.00               | 301                   | 36                     |
            | EA124        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T14:00:00 | 2025-01-02T09:00:00 | 1200.00              | 4500.00               | 301                   | 36                     |

    Scenario: No flights available for the given route and date
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-02T10:00:00 | 2025-01-03T05:00:00 | 1200.00      | 4500.00       |
        When I search for flights from YVR to CDG on 2025-01-01
        Then no flights are returned

    Scenario: Search for flights with no matching route
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | LHR                        | Europe/London              | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1000.00      | 4000.00       |
        When I search for flights from YVR to CDG on 2025-01-01
        Then no flights are returned

    Scenario: Search with invalid date format
        When I search for flights from YVR to CDG on 2025-01-100
        Then an error message is returned which states that the date format is invalid

    Scenario: Search with lowercase airport codes
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       |
        When I search for flights from yvr to cdg on 2025-01-01
        Then the following flights are returned:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | CheapestEconomyPrice | CheapestBusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00              | 4500.00               | 301                   | 36                     |

    Scenario: Search with mixed case airport codes
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       |
        When I search for flights from YvR to CdG on 2025-01-01
        Then the following flights are returned:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | CheapestEconomyPrice | CheapestBusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00              | 4500.00               | 301                   | 36                     |

    Scenario: Search with missing departure airport code
        When I search for flights from  to CDG on 2025-01-01
        Then an error message is returned which states that the departure airport code is required

    Scenario: Search with missing destination airport code
        When I search for flights from YVR to  on 2025-01-01
        Then an error message is returned which states that the destination airport code is required

    Scenario: Search with the same departure and destination airports
        When I search for flights from YVR to YVR on 2025-01-01
        Then an error message is returned which states that the departure and destination airports cannot be the same

    Scenario: Search when some flights have already departed
        Given the following flights exist:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice |
            | EA123        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2024-01-01T10:00:00 | 2024-01-02T05:00:00 | 1200.00      | 4500.00       |
            | EA124        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T14:00:00 | 2025-01-02T09:00:00 | 1200.00      | 4500.00       |
        When I search for flights from YVR to CDG on 2025-01-01
        Then the following flights are returned:
            | FlightNumber | DepartureAirportIataCode | DepartureAirportTimeZone | DestinationAirportIataCode | DestinationAirportTimeZone | DepartureTime       | ArrivalTime         | CheapestEconomyPrice | CheapestBusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
            | EA124        | YVR                      | America/Vancouver        | CDG                        | Europe/Paris               | 2025-01-01T14:00:00 | 2025-01-02T09:00:00 | 1200.00              | 4500.00               | 301                   | 36                     |

    Scenario: Search with missing date
        When I search for flights from YVR to CDG on
        Then an error message is returned which states that the date parameter is required
