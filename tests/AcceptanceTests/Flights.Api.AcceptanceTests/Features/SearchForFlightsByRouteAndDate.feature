Feature: Search for flights by route and date
  As a customer
  I want to search for flights by route and date
  So that I can evaluate the options available to me

  Scenario: Search for flights by route and date with valid results
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2020-01-01T10:00:00 | 2020-01-02T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
      | EA124        | YVR              | CDG            | 2020-01-01T14:00:00 | 2020-01-02T09:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
      | EA125        | YVR              | LHR            | 2020-01-01T09:00:00 | 2020-01-02T04:00:00 | 1000.00      | 4000.00       | 60                    | 15                     |
    When I search for flights from YVR to CDG on 2020-01-01
    Then the following flights are returned:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2020-01-01T10:00:00 | 2020-01-02T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
      | EA124        | YVR              | CDG            | 2020-01-01T14:00:00 | 2020-01-02T09:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |

  Scenario: No flights available for the given route and date
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | CDG            | 2020-01-02T10:00:00 | 2020-01-03T05:00:00 | 1200.00      | 4500.00       | 50                    | 10                     |
    When I search for flights from YVR to CDG on 2020-01-01
    Then no flights are returned

  Scenario: Search for flights on a different date but same route
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA126        | YVR              | CDG            | 2020-01-02T10:00:00 | 2020-01-03T05:00:00 | 1300.00      | 4600.00       | 45                    | 8                      |
    When I search for flights from YVR to CDG on 2020-01-02
    Then the following flights are returned:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA126        | YVR              | CDG            | 2020-01-02T10:00:00 | 2020-01-03T05:00:00 | 1300.00      | 4600.00       | 45                    | 8                      |

  Scenario: Search for flights with no matching route
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime       | ArrivalTime         | EconomyPrice | BusinessPrice | AvailableEconomySeats | AvailableBusinessSeats |
      | EA123        | YVR              | LHR            | 2020-01-01T10:00:00 | 2020-01-02T05:00:00 | 1000.00      | 4000.00       | 60                    | 15                     |
    When I search for flights from YVR to CDG on 2020-01-01
    Then no flights are returned
