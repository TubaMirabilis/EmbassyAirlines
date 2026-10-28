# Shared Libraries

The `Shared` project and its companion `Shared.*` projects hold the code that the Airports, Aircraft, and Flights services share instead of each defining their own: contracts, the domain-event base entity, guard clauses, the transactional outbox, PostgreSQL wiring, host defaults, the HTTP pipeline, and SQS batch handling.

Until October 2026 the contracts, outbox primitives, and HTTP plumbing all lived in `Shared`. They are now split by dependency footprint, so `Shared` itself has no package dependencies and each Lambda builds against only what it uses. A migration Lambda, for example, takes neither ASP.NET Core nor OpenTelemetry.

| Project                        | Contents                                                                           | Depends on                                                                                 | Used by                                       |
| ------------------------------ | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ | --------------------------------------------- |
| `Shared`                       | Contracts, domain events, `Entity`, `Ensure`                                       | nothing                                                                                    | every service's `X.Core`, and so every Lambda |
| `Shared.TransactionalOutbox`   | `OutboxMessage`, `OutboxConstants`, `OutboxJson`, the outbox processor base        | `Microsoft.Extensions.Logging`                                                             | `Shared.Npgsql`                               |
| `Shared.Npgsql`                | EF Core/PostgreSQL wiring, the outbox interceptor, the outbox drain loop           | `Shared`, `Shared.TransactionalOutbox`, Npgsql, AWS SDK (RDS)                              | every service's `X.Infrastructure`            |
| `Shared.ServiceDefaults`       | `AddServiceDefaults`: OpenTelemetry, service discovery, `HttpClient` resilience    | OpenTelemetry and `Microsoft.Extensions.*` packages                                        | API, publisher, and message-handler Lambdas   |
| `Shared.HttpApiLambdaDefaults` | HTTP API Lambda hosting, endpoint discovery, error handling, logging, test helpers | `Shared.ServiceDefaults`, ASP.NET Core, Lambda hosting, ErrorOr, FluentValidation, Serilog | API Lambdas and their functional tests        |
| `Shared.Messaging`             | `SqsBatchProcessor`                                                                | `Amazon.Lambda.Core`, `Amazon.Lambda.SQSEvents`                                            | message-handler Lambdas                       |

Each Lambda's dockerfile in `docker/` copies exactly the projects it builds against, so adding a project reference to a Lambda also means adding a matching `COPY` line to its dockerfile.

---

## `Shared`

### Shared contracts (DTOs)

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

The folder also holds `MigrationRequest` and `MigrationResponse`, the CloudFormation custom-resource payloads that each service's Migrations Lambda receives and returns.

### Domain events

The Embassy Airlines system uses domain events, message broker-dispatched integration events and relies on eventually consistent communication. Consequently, the Shared Library contains a substantial number of event contracts.

Examples include:

- AircraftCreatedEvent
- AirportUpdatedEvent
- FlightScheduledEvent
- FlightArrivedEvent
- FlightCancelledEvent
- FlightDelayedEvent
- FlightPricingAdjustedEvent

These immutable record types implement `IDomainEvent` (or the more specific `IFlightStatusManagementEvent`, which adds a `FlightId`) and represent domain events that are persisted to the Transactional Outbox before being asynchronously published between services.

### Base entity

The project contains a reusable `Entity` base class.

It stores domain events raised by an entity:

- AddDomainEvent()
- ClearDomainEvents()
- DomainEvents collection

This is a common Domain-Driven Design pattern where entities accumulate events during a transaction, which are published after persistence. `InsertOutboxMessagesInterceptor` in `Shared.Npgsql` collects these events from every tracked entity when the context saves, and clears them once the save succeeds.

### Validation helpers

The `Ensure` class centralizes guard clauses:

- `NotEmpty(Guid)`
- `NotNullOrEmpty(string)`
- `NotNull<T>(T)`
- `ZeroOrGreater(decimal)`
- `GreaterThanZero(int)`

It also uses `CallerArgumentExpression`, allowing exceptions to automatically include the caller's parameter name without manually specifying it.

---

## `Shared.TransactionalOutbox`

The project provides the database- and publisher-agnostic half of the **Transactional Outbox** pattern, allowing services to reliably publish integration events after database transactions have been committed. It knows nothing about PostgreSQL or SNS.

The components include:

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

- `OutboxProcessorBase<TPublisher>`, an abstract base class that owns the per-message processing lifecycle (resolve publisher, publish, mark as processed, register failures) and holds the publisher instance that publish delegates are invoked with. It provides:

    - exponential retry backoff,
    - retry scheduling,
    - dead-letter handling after repeated or unrecoverable failures (a message type with no registered publisher, a `JsonException`, or a `NotSupportedException`),
    - consistent structured logging.

    The type parameter keeps the shared abstraction decoupled from any particular publisher type.

- `OutboxJson`, which deserializes an outbox payload with web defaults for publish delegates to use, and throws a `JsonException` (an unrecoverable failure) when a payload deserializes to `null`.

- `IOutboxProcessor`, the `ProcessAsync` contract that each Publisher Lambda resolves and invokes.

- `OutboxConstants`, which centralises shared processing configuration such as batch size, retry limits, retry delays, how long a claim on a message stays valid and how much of that claim is reserved as a safety margin.

---

## `Shared.Npgsql`

Everything that touches PostgreSQL or EF Core:

- `AddDatabaseConnection<TDbContext>`, with one overload that reads `DbConnection:*` configuration and authenticates to RDS Proxy with IAM tokens, and another that takes a plain connection string for the tests. Both use snake_case naming and a per-schema `__EFMigrationsHistory` table, and can enable NodaTime.
- `AddOutboxInterceptor` and `InsertOutboxMessagesInterceptor`, described below.
- `NpgsqlOutboxProcessorBase<TPublisher>`, the outbox drain loop, also described below.
- `ApplyOutboxConfiguration`, which maps `outbox_messages` and registers `DatabaseClock.UtcNow()` as PostgreSQL's built-in `now()`.
- `ApplyMigrationsAsync`, which applies migrations at startup in the `Development` environment.

### Writing to the outbox

`InsertOutboxMessagesInterceptor` is an EF Core `SaveChangesInterceptor`. As a context saves, synchronously or asynchronously, the interceptor turns every domain event on every tracked `Entity` into an `OutboxMessage`, named after the event type and serialized as JSON. It skips any event whose id is already tracked as an outbox message. Those rows are inserted by the same `SaveChanges` call as the state change, so they commit or roll back together. After a successful save, the interceptor clears the entities' events.

The interceptor is opt-in. `AddOutboxInterceptor` registers it, and `AddDatabaseConnection` attaches it to the `DbContext` only if it is registered. The API Lambdas are the only code paths that raise events, so they are the ones that call `AddDatabaseConnection(...).AddOutboxInterceptor()`.

### Draining the outbox

Relational services do not inherit from `OutboxProcessorBase<TPublisher>` directly. `NpgsqlOutboxProcessorBase<TPublisher>` sits between them and implements `IOutboxProcessor`, owning the drain loop: it claims a batch of due messages with a single statement — a `SELECT ... FOR UPDATE SKIP LOCKED` CTE feeding an `UPDATE ... RETURNING` that stamps a freshly generated claim identifier and a claim expiry, so a second invocation cannot take the same rows — and only then publishes each message and persists that message's outcome on its own. Publishing therefore never happens with a row lock or an open transaction held, and one message's failure cannot roll back the outcomes already recorded for the rest of the batch.

The claim identifier gives the lease explicit ownership rather than leaving ownership implied by wall-clock timing, and PostgreSQL rather than the worker decides when that ownership begins and ends:

- Both ends of the lease are timed by the database. The expiry is stamped as `now() + OutboxConstants.ClaimDuration` inside the claiming statement, and the check that it has not passed is `claimed_until_utc > now()`, written in LINQ as `DatabaseClock.UtcNow()` — a mapping onto the built-in `now()` that `ApplyOutboxConfiguration` registers alongside the entity configuration. `claimed_until_utc` is shared state that concurrent invocations coordinate through, so a worker whose clock had drifted would otherwise grant itself a lease of the wrong length from PostgreSQL's point of view, or misjudge whether it still held one.
- The `UPDATE` that records a message's outcome is issued as a single statement conditioned on both the claim identifier and an unexpired `claimed_until_utc`. It therefore matches only while this invocation still holds a live lease. Checking the claim identifier alone would not be enough: a publish that overran the lease could still record its outcome for as long as no other invocation had happened to reclaim the row yet, which left the write's correctness dependent on timing. If the statement matches nothing — the lease expired, or another invocation reclaimed the row — the stale invocation is told it has lost the claim and discards its outcome instead of writing state it no longer owns.
- Claiming is one statement, so no other invocation can slip between the `SELECT` that picks the rows and the `UPDATE` that stamps them; `claim_id` is additionally mapped as a concurrency token, which guards any tracked update of an outbox row.
- Before each publish the loop checks how much of the lease it has spent — as elapsed time measured locally, not as a comparison between two clocks — and stops the batch once it is within `OutboxConstants.ClaimSafetyMargin` of `ClaimDuration`, so a worker does not keep publishing on a lease it is about to lose. That is an optimisation rather than part of the correctness boundary: it reduces how often a publish overruns its lease, but cannot guarantee it, which is why the outcome write re-checks the lease against the database clock.

Together these mean correctness no longer depends on the Publisher Lambda's configured timeout staying shorter than `OutboxConstants.ClaimDuration`, nor on the clocks of the machines running it; that sizing now only affects how much of a batch a single invocation gets through. Messages whose claim expires or is surrendered before an outcome is recorded — because the invocation was killed, cancelled, ran out of lease, or lost ownership — become eligible again on a later run. A message can still be published and then have its outcome discarded, so delivery remains at-least-once and consumers must be idempotent; what the lease rules out is an invocation writing a message's state after it has stopped owning it.

A failure to persist an outcome ends the batch as well, not just the message it happened to. Duplicates around the publish/record boundary are unavoidable, but once the database has shown that outcomes cannot be written there is nothing to gain from publishing the rest of the claim: every one of those messages would be published now and published again after the lease expired. Abandoning them leaves them unattempted instead, and a later invocation retries them cleanly.

Concrete services implement their own processors by inheriting from that base, passing their PostgreSQL schema name to its constructor (the base validates and quotes it, then embeds it in the claiming statement, since identifiers cannot be bound as parameters), and overriding `ResolvePublisher`, which maps a message type name to a `Func<TPublisher, string, CancellationToken, Task>` that publishes it (returning `null` when no publisher is registered, which dead-letters the message). Because the resolved delegate takes the publisher as a parameter rather than capturing it, services can return cached static delegates and avoid allocating a closure per message. Each service therefore determines how its messages are dispatched while reusing a common processing lifecycle and retry and failure-handling strategy.

---

## `Shared.ServiceDefaults`

`AddServiceDefaults()` is the host setup that every API, publisher, and message-handler Lambda calls; the migration Lambdas do not. It is generic over `IHostApplicationBuilder`, so it works on a `WebApplicationBuilder` and a plain `HostApplicationBuilder` alike. It configures:

- OpenTelemetry logging, metrics, and tracing, with ASP.NET Core, `HttpClient`, runtime, AWS SDK, AWS.Messaging, and Lambda instrumentation, and a trace source named after the application. No exporter is registered; see Observability in the [top-level README](../../README.md#observability).
- Service discovery.
- The standard resilience handler on every `HttpClient` created through `IHttpClientFactory`.

---

## `Shared.HttpApiLambdaDefaults`

The ASP.NET Core layer, used only by the API Lambdas. It references `Shared.ServiceDefaults`.

### Host wiring

`AddHttpApiLambdaDefaults(assembly)` calls `AddServiceDefaults`, then discovers the assembly's endpoints, hosts the app as a Lambda behind API Gateway with `AddAWSLambdaHosting(LambdaEventSource.HttpApi)`, and registers `GlobalExceptionHandler`, `ProblemDetails`, and OpenAPI. `UseDefaultPipeline()` adds the exception handler, the correlation-id middleware, and Serilog request logging. `MapDefaultEndpoints()` maps the discovered endpoints, plus the OpenAPI document in `Development`. Each API still configures its own environment-variable prefix and Serilog logger, so its `Program.cs` follows this shape (taken from Airports):

```csharp
var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config.AddEnvironmentVariables(prefix: "AIRPORTS_");
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
builder.AddHttpApiLambdaDefaults(typeof(Program).Assembly);
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseConnection<ApplicationDbContext>(config, false, "airports").AddOutboxInterceptor();
}
// service-specific registrations
var app = builder.Build().UseDefaultPipeline().MapDefaultEndpoints();
if (app.Environment.IsDevelopment())
{
    await app.ApplyMigrationsAsync<ApplicationDbContext>();
}
await app.RunAsync();
```

### Error handling

The library standardizes API error responses.

It includes:

- `ErrorHandlingHelper`
- `GlobalExceptionHandler`
- `ProblemDetails` extension methods

`ErrorHandlingHelper.GetProblemDetails` maps `ErrorOr` validation, not-found, and conflict errors to RFC-compliant `ProblemDetails` responses (`400`, `404`, and `409`) with consistent titles. `GlobalExceptionHandler` logs unexpected exceptions and returns them as HTTP 500 `application/problem+json` responses.

Exception handlers run in registration order, so a service can handle a specific exception before `GlobalExceptionHandler` sees it by registering its own handler before calling `AddHttpApiLambdaDefaults`. Flights does this with `ConcurrencyExceptionHandler`, which turns a `DbUpdateConcurrencyException` into a `409 Conflict`.

### Endpoint discovery

Instead of manually registering every Minimal API endpoint, the project defines:

```csharp
public interface IEndpoint
```

Each feature implements this interface, and the extension methods automatically:

- discover endpoint classes via reflection,
- register them with dependency injection,
- map them during application startup.

This pattern helps to control the cleanliness of top-level code in Web API projects.

### Validation extensions

A small helper, `IsValid(out string formattedErrors)`, converts `FluentValidation` results into a newline-separated string of error messages, simplifying error reporting.

### Logging middleware

`RequestContextLoggingMiddleware` adds a correlation ID to the Serilog logging context.

It:

- reads `X-Correlation-Id` if supplied,
- otherwise falls back to ASP.NET's trace identifier,
- enriches all logs for the request with that ID.

This makes tracing requests across multiple services much easier.

### HTTP testing helpers

The project also provides extension methods for the functional tests: `ReadProblemDetailsAsync` deserializes `ProblemDetails` from an `HttpResponseMessage`, and `CreateExpectedProblemDetails` builds the expected `ProblemDetails` for a `400`, `404`, or `409` status code.

---

## `Shared.Messaging`

`SqsBatchProcessor.ProcessAsync` is the batch loop shared by all six message-handler Lambdas. It passes each SQS record to the handler's per-message delegate in turn, catches and logs any exception, and returns an `SQSBatchResponse` listing only the failed message IDs. With `ReportBatchItemFailures` enabled on the event source mapping, which the `EventHandlerLambda` CDK construct does, Lambda retries those messages individually rather than the whole batch.
