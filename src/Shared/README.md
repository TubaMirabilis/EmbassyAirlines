# Shared Library

The Shared project exposes utility APIs which have been designed to support the microservices architecture of the Embassy Airlines system and its various bounded contexts, including aircraft management, airport management, flight scheduling and flight status management.

Instead of each service defining its own DTOs, events, middleware, validation helpers, and endpoint infrastructure, they all share these definitions.

---

## Shared contracts (DTOs)

The largest part of the project is the **Contracts** folder.

Examples include:

- `AircraftDto`
- `AirportDto`
- `FlightDto`
- `SeatDto`

along with request DTOs such as:

- `CreateAircraftDto`
- `ScheduleFlightDto`
- `AssignAircraftToFlightDto`
- `RescheduleFlightDto`

These are immutable record types used for communication between APIs or services rather than representing database entities. For example, `ScheduleFlightDto` contains all the information required to schedule a flight, including airports, aircraft, prices, flight numbers, and scheduling policy.

---

## Domain events

The Embassy Airlines system uses domain events, message Broker-dispatched integration events and relies on eventually consistent communication. Consequently, the Shared Library contains a substantial number of event contracts.

Examples include:

- AircraftCreatedEvent
- AirportUpdatedEvent
- FlightScheduledEvent
- FlightArrivedEvent
- FlightCancelledEvent
- FlightDelayedEvent
- FlightPricingAdjustedEvent

These records implement `IDomainEvent` (or the more specific `IFlightStatusManagementEvent`) and are intended for event-driven communication between services.

---

## Base entity

The project contains a reusable `Entity` base class.

It stores domain events raised by an entity:

- AddDomainEvent()
- ClearDomainEvents()
- DomainEvents collection

This is a common Domain-Driven Design pattern where entities accumulate events during a transaction, which are published after persistence.

---

## Outbox support

To improve the reliability of event delivery, the Outbox pattern was implemented in selected services to ensure that domain state changes and integration events are persisted atomically before asynchronous publication. The OutboxMessage record supports a common format for outbox messages.

It stores:

- message ID
- serialized content
- creation time
- retry count
- processed timestamp
- dead-letter timestamp

An accompanying `IOutboxProcessor` interface defines a template for services which carry out asynchronous processing of outbox messages.

---

## Validation helpers

The `Ensure` class centralizes guard clauses such as:

- NotEmpty(Guid)
- NotNullOrEmpty(string)
- GreaterThanZero(int)
- ZeroOrGreater(decimal)
- LessThanOrEqualTo(...)

It also uses `CallerArgumentExpression`, allowing exceptions to automatically include the caller's parameter name without manually specifying it.

---

## Error handling

The library standardizes API error responses.

It includes:

- `ErrorHandlingHelper`
- `GlobalExceptionHandler`
- `ProblemDetails` extension methods

Validation, conflict, and not-found errors are mapped to RFC-compliant `ProblemDetails` responses with consistent titles and status codes, while unexpected exceptions are logged and returned as HTTP 500 responses.

---

## Endpoint discovery

Instead of manually registering every Minimal API endpoint, the project defines:

```csharp
public interface IEndpoint
```

Each feature implements this interface, and the extension methods automatically:

- discover endpoint classes via reflection,
- register them with dependency injection,
- map them during application startup.

This pattern helps to control the clemliness of top-level code in Web API projects.

---

## HTTP testing helpers

The Shared Library provides useful extension methods for integration tests, including methods to deserialize ProblemDetails from an HttpResponseMessage and construct expected ProblemDetails for assertions based on HTTP status codes.

---

## Validation extensions

A small helper converts `FluentValidation` results into a formatted string of error messages, simplifying error reporting.

---

## Logging middleware

`RequestContextLoggingMiddleware` adds a correlation ID to the Serilog logging context.

It:

- reads `X-Correlation-Id` if supplied,
- otherwise falls back to ASP.NET's trace identifier,
- enriches all logs for the request with that ID.

This makes tracing requests across multiple services much easier.
