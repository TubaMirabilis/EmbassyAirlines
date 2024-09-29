Feature: Search for flights by route and date
  As a customer
  I want to search for flights by route and date
  So that I can evaluate the options available to me

  Scenario: Search for flights by route and date
    Given the following flights exist:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime | ArrivalTime | Price |
      | 1            | LHR              | CDG            | 2020-01-01T10:00:00 | 2020-01-01T12:00:00 | 100.00 |
      | 2            | CDG              | LHR            | 2020-01-01T14:00:00 | 2020-01-01T16:00:00 | 100.00 |
    When I search for flights from "LHR" to "CDG" on "2020-01-01"
    Then the following flights should be returned:
      | FlightNumber | DepartureAirport | ArrivalAirport | DepartureTime | ArrivalTime | Price |
      | 1            | LHR              | CDG            | 2020-01-01T10:00:00 | 2020-01-01T12:00:00 | 100.00 |