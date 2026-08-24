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
    - claim expiry timestamp,
    - claim identifier,
    - last processing error.

- `OutboxProcessorBase`, an abstract base class that provides publisher-agnostic processing behaviour, including:

    - JSON deserialization helpers,
    - exponential retry backoff,
    - retry scheduling,
    - dead-letter handling after repeated or unrecoverable failures,
    - consistent structured logging.

- `OutboxProcessorBase<TPublisher>`, which extends it with the per-message processing lifecycle (resolve publisher, publish, mark as processed, register failures) and holds the publisher instance that publish delegates are invoked with. The type parameter keeps the shared abstraction decoupled from any particular publisher type.

- `OutboxConstants`, which centralises shared processing configuration such as batch size, retry limits, retry delays, how long a claim on a message stays valid and how much of that claim is reserved as a safety margin.

Relational services do not inherit from `OutboxProcessorBase<TPublisher>` directly. `Shared.Npgsql.NpgsqlOutboxProcessorBase<TPublisher>` sits between them and implements `IOutboxProcessor`, owning the drain loop: it claims a batch of due messages with a single statement — a `SELECT ... FOR UPDATE SKIP LOCKED` CTE feeding an `UPDATE ... RETURNING` that stamps a freshly generated claim identifier and a claim expiry, so a second invocation cannot take the same rows — and only then publishes each message and persists that message's outcome on its own. Publishing therefore never happens with a row lock or an open transaction held, and one message's failure cannot roll back the outcomes already recorded for the rest of the batch.

The claim identifier gives the lease explicit ownership rather than leaving ownership implied by wall-clock timing, and PostgreSQL rather than the worker decides when that ownership begins and ends:

- Both ends of the lease are timed by the database. The expiry is stamped as `now() + OutboxConstants.ClaimDuration` inside the claiming statement, and the check that it has not passed is `claimed_until_utc > now()`, written in LINQ as `DatabaseClock.UtcNow()` — a mapping onto the built-in `now()` that `ApplyOutboxConfiguration` registers alongside the entity configuration. `claimed_until_utc` is shared state that concurrent invocations coordinate through, so a worker whose clock had drifted would otherwise grant itself a lease of the wrong length from PostgreSQL's point of view, or misjudge whether it still held one.
- The `UPDATE` that records a message's outcome is issued as a single statement conditioned on both the claim identifier and an unexpired `claimed_until_utc`. It therefore matches only while this invocation still holds a live lease. Checking the claim identifier alone would not be enough: a publish that overran the lease could still record its outcome for as long as no other invocation had happened to reclaim the row yet, which left the write's correctness dependent on timing. If the statement matches nothing — the lease expired, or another invocation reclaimed the row — the stale invocation is told it has lost the claim and discards its outcome instead of writing state it no longer owns.
- Claiming is one statement, so no other invocation can slip between the `SELECT` that picks the rows and the `UPDATE` that stamps them; `claim_id` is additionally mapped as a concurrency token, which guards any tracked update of an outbox row.
- Before each publish the loop checks how much of the lease it has spent — as elapsed time measured locally, not as a comparison between two clocks — and stops the batch once it is within `OutboxConstants.ClaimSafetyMargin` of `ClaimDuration`, so a worker does not keep publishing on a lease it is about to lose. That is an optimisation rather than part of the correctness boundary: it reduces how often a publish overruns its lease, but cannot guarantee it, which is why the outcome write re-checks the lease against the database clock.

Together these mean correctness no longer depends on the Publisher Lambda's configured timeout staying shorter than `OutboxConstants.ClaimDuration`, nor on the clocks of the machines running it; that sizing now only affects how much of a batch a single invocation gets through. Messages whose claim expires or is surrendered before an outcome is recorded — because the invocation was killed, cancelled, ran out of lease, or lost ownership — become eligible again on a later run. A message can still be published and then have its outcome discarded, so delivery remains at-least-once and consumers must be idempotent; what the lease rules out is an invocation writing a message's state after it has stopped owning it.

A failure to persist an outcome ends the batch as well, not just the message it happened to. Duplicates around the publish/record boundary are unavoidable, but once the database has shown that outcomes cannot be written there is nothing to gain from publishing the rest of the claim: every one of those messages would be published now and published again after the lease expired. Abandoning them leaves them unattempted instead, and a later invocation retries them cleanly.

Concrete services implement their own processors by inheriting from that base and overriding `ClaimEligibleMessagesAsync` (the schema-specific claiming statement) and `ResolvePublisher`, which maps a message type name to a `Func<TPublisher, string, CancellationToken, Task>` that publishes it (returning `null` when no publisher is registered, which dead-letters the message). Because the resolved delegate takes the publisher as a parameter rather than capturing it, services can return cached static delegates and avoid allocating a closure per message. Each service therefore determines how its messages are dispatched while reusing a common processing lifecycle and retry and failure-handling strategy.

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
