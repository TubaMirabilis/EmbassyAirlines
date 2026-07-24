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

The Embassy Airlines system uses domain events, message broker-dispatched integration events and relies on eventually consistent communication. Consequently, the Shared Library contains a substantial number of event contracts.

Examples include:

- AircraftCreatedEvent
- AirportUpdatedEvent
- FlightScheduledEvent
- FlightArrivedEvent
- FlightCancelledEvent
- FlightDelayedEvent
- FlightPricingAdjustedEvent

These immutable record types implement `IDomainEvent` (or the more specific `IFlightStatusManagementEvent`) and represent domain events that are persisted to the Transactional Outbox before being asynchronously published between services.

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

The Shared library provides common infrastructure for implementing the **Transactional Outbox** pattern, allowing services to reliably publish integration events after database transactions have been committed.

The shared components include:

- `OutboxMessage`, which represents a persisted outbox entry containing:

    - message identifier,
    - serialized event payload,
    - event type,
    - creation timestamp,
    - retry metadata,
    - processing timestamp,
    - dead-letter timestamp,
    - last processing error.

- `OutboxProcessorBase`, an abstract base class that provides common processing behaviour, including:

    - JSON deserialization helpers,
    - exponential retry backoff,
    - retry scheduling,
    - dead-letter handling after repeated or unrecoverable failures,
    - consistent structured logging.

- `OutboxConstants`, which centralises shared processing configuration such as batch size, retry limits and retry delays.

Concrete services implement their own processors by inheriting from `OutboxProcessorBase`, allowing each service to determine how messages are dispatched while reusing a common retry and failure-handling strategy.

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

This pattern helps to control the cleanliness of top-level code in Web API projects.

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
