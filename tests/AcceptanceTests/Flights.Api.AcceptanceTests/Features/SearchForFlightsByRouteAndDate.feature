Feature: Search for flights by route and date
  As a customer
  I want to search for flights by route and date
  So that I can evaluate the options available to me

  Scenario: Search for flights by route and date with valid results
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
      | EA124        | YVR              | CDG            | 2025-01-01T14:00:00 | 2025-01-02T09:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
      | EA125        | YVR              | LHR            | 2025-01-01T09:00:00 | 2025-01-02T04:00:00 | 1000.00      | 4000.00       | 60                    | 15                     |
    When I search for flights from YVR to CDG on 2025-01-01
    Then the following flights are returned:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
      | EA124        | YVR              | CDG            | 2025-01-01T14:00:00 | 2025-01-02T09:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |

  Scenario: No flights available for the given route and date
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2025-01-02T10:00:00 | 2025-01-03T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
    When I search for flights from YVR to CDG on 2025-01-01
    Then no flights are returned

  Scenario: Search for flights with no matching route
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | LHR            | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1000.00      | 4000.00       | 60                    | 15                     |
    When I search for flights from YVR to CDG on 2025-01-01
    Then no flights are returned

  Scenario: Search with invalid date format
    When I search for flights from YVR to CDG on 2025-01-100
    Then an error message is returned

  Scenario: Search with lowercase airport codes
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
    When I search for flights from yvr to cdg on 2025-01-01
    Then the following flights are returned:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2025-01-01T10:00:00 | 2025-01-02T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |

  Scenario: Search with missing departure airport code
    When I search for flights from  to CDG on 2025-01-01
    Then an error message is returned

  Scenario: Search with missing arrival airport code
    When I search for flights from YVR to  on 2025-01-01
    Then an error message is returned

  Scenario: Search with the same departure and arrival airports
    When I search for flights from YVR to YVR on 2025-01-01
    Then an error message is returned

    Scenario: All flights have already departed
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2024-01-01T10:00:00 | 2024-01-02T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
    When I search for flights from YVR to CDG on 2025-01-03
    Then no flights are returned
