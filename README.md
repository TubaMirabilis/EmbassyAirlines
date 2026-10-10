# Embassy Airlines

A demonstration event-driven microservices backend built on **.NET 10** and **AWS**. It models an airline operations domain across three bounded contexts — **Airports**, **Aircraft**, and **Flights** — which communicate asynchronously over SNS topics and SQS queues and are **eventually consistent**.

Every runtime component ships as a Docker-image AWS Lambda. Infrastructure is provisioned with AWS CDK, also written in C#. The target region is `eu-west-2`.

> This is a portfolio/demonstration project. Some choices (one shared PostgreSQL instance, a single deployed environment, no NAT gateways) trade production-grade isolation for a sane infrastructure bill — these are called out in [Deliberate trade-offs](#deliberate-trade-offs).

---

## Table of contents

- [What it does](#what-it-does)
- [Architecture](#architecture)
- [The bounded contexts](#the-bounded-contexts)
- [Event choreography](#event-choreography)
- [The transactional outbox](#the-transactional-outbox)
- [HTTP API reference](#http-api-reference)
- [Domain rules worth knowing](#domain-rules-worth-knowing)
- [Repository layout](#repository-layout)
- [Getting started](#getting-started)
- [Build, test, and tooling](#build-test-and-tooling)
- [Code-quality enforcement](#code-quality-enforcement)
- [Configuration](#configuration)
- [Database and migrations](#database-and-migrations)
- [Deployment](#deployment)
- [Observability](#observability)
- [Testing strategy](#testing-strategy)
- [Deliberate trade-offs](#deliberate-trade-offs)
- [Extending the system](#extending-the-system)

---

## What it does

The system supports the core workflow of scheduling and operating a flight:

1. **Airports** are registered with an ICAO code, IATA code, name, and IANA time zone.
2. **Aircraft** are created against an equipment code (e.g. `B78X`). The seat map is not supplied in the request — the API fetches a **seat-layout definition from S3** and expands it into individual seats.
3. **Flights** are scheduled between two airports using **local times**, which are resolved to instants against each airport's time zone.
4. Flights move through a **status lifecycle** (scheduled → en route → arrived, with delay and cancellation paths), and each transition emits an event.
5. The Aircraft context **reacts to those flight events** to keep aircraft location state current — an aircraft is marked en route on departure and parked at the arrival airport when the flight lands.

Nothing in step 5 is a synchronous call. The Flights context never invokes the Aircraft context; it records an event and moves on.

---

## Architecture

```
                               API Gateway HTTP API (custom domain, /api)
                                                │
                 ┌──────────────────────────────┼──────────────────────────────┐
                 │                              │                              │
          ┌──────▼──────┐                ┌──────▼──────┐                ┌──────▼──────┐
          │  Airports   │                │  Aircraft   │                │   Flights   │
          │  API Lambda │                │  API Lambda │                │  API Lambda │
          └──────┬──────┘                └──────┬──────┘                └──────┬──────┘
                 │                              │                              │
          ┌──────▼──────────────────────────────▼──────────────────────────────▼──────┐
          │              RDS Proxy (IAM auth) → one shared PostgreSQL 18              │
          │     airports schema           aircraft schema           flights schema    │
          │     + outbox_messages         + outbox_messages         + outbox_messages │
          └──────┬──────────────────────────────┬──────────────────────────────┬──────┘
                 │                              │                              │
          ┌──────▼──────┐                ┌──────▼──────┐                ┌──────▼──────┐
          │  Airports   │                │  Aircraft   │                │   Flights   │
          │  Publisher  │                │  Publisher  │                │  Publisher  │
          │ every 1 min │                │ every 1 min │                │ every 1 min │
          └──────┬──────┘                └──────┬──────┘                └──────┬──────┘
                 │                              │                              │
                 └──────────────────────────────┼──────────────────────────────┘
                                                │
                                        ┌───────▼───────┐
                                        │  SNS  topics  │
                                        └───────┬───────┘
                                                │
                                        ┌───────▼───────┐
                                        │  SQS  queues  │──▶ DLQ
                                        └───────┬───────┘
                                                │
                                     Message-handler Lambdas
                                     (one per consumed event)
```

Not shown: each service also has a **Migrations Lambda** that CloudFormation invokes during `cdk deploy` to apply that service's EF Core migrations before anything that depends on the schema is updated. See [Database and migrations](#database-and-migrations).

### Per-service layering

All three services follow the same layered structure:

| Project                                    | Responsibility                                                                                                                                                                                             |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `X.Core`                                   | Domain aggregates with real behaviour (`Airport.Update`, `Aircraft.Create`, `flight.AdjustStatus`). Entities extend `Entity` and raise domain events via `AddDomainEvent`. No infrastructure dependencies. |
| `X.Infrastructure`                         | EF Core `ApplicationDbContext`, entity configurations, migrations, `DesignTimeDbContextFactory`, and the service's `OutboxProcessor` (its claim query and publisher registry).                             |
| `X.Api.Lambda`                             | ASP.NET Core Minimal API hosted in Lambda via `AddHttpApiLambdaDefaults`, which calls `AddAWSLambdaHosting(LambdaEventSource.HttpApi)`. HTTP endpoints only.                                               |
| `X.Publisher.Lambda`                       | The outbox drainer: a plain `LambdaBootstrapBuilder` handler invoked on a one-minute EventBridge schedule.                                                                                                 |
| `X.Migrations.Lambda`                      | Applies the service's EF Core migrations. It is the handler behind a CloudFormation custom resource, so migrations run as part of a deployment.                                                            |
| `X.Api.Lambda.MessageHandlers.<EventName>` | One Lambda project **per consumed event**. Each is an SQS-triggered `Function` class that builds its own `HostApplicationBuilder`. Airports consumes no events, so it has none.                            |

Splitting message handlers one-per-event means each consumer scales, fails, retries, and dead-letters independently — a poison message on `FlightArrived` cannot stall `FlightMarkedAsEnRoute`.

### Shared libraries

Four projects hold the cross-cutting code every service depends on.

**`src/Shared`** — contracts and framework-agnostic building blocks. See [`src/Shared/README.md`](src/Shared/README.md) for the full tour. The pieces you touch most often:

- **`Shared.Contracts`** — immutable record DTOs (`AircraftDto`, `ScheduleFlightDto`, …) and integration events (`AircraftCreatedEvent`, `FlightArrivedEvent`, …) implementing `IDomainEvent` / `IFlightStatusManagementEvent`. **These records are the wire contract between services** — changing one affects both producer and consumer, and they deploy independently.
- **`IEndpoint`** — every Minimal API endpoint is a class implementing `MapEndpoint`. `AddEndpoints(assembly)` + `MapEndpoints()` discover and register them by reflection, so a new endpoint needs no manual registration.
- **`Ensure`** — guard-clause helpers (`NotNullOrEmpty`, `GreaterThanZero`, …) using `CallerArgumentExpression`, so the failing parameter name comes for free. Prefer these over hand-written argument checks.
- **Error handling** — endpoints return `ErrorOr`-based errors mapped through `ErrorHandlingHelper.GetProblemDetails`; `GlobalExceptionHandler` catches the rest. Everything surfaces as RFC-compliant `ProblemDetails`. Follow the existing endpoint pattern rather than throwing.
- **`RequestContextLoggingMiddleware`** — enriches Serilog logs with a correlation id from the `X-Correlation-Id` header, falling back to the trace id.
- **Outbox primitives** — `Entity` (the domain-event list), `OutboxMessage`, `OutboxConstants`, and `OutboxProcessorBase<TPublisher>`, which owns the publish/retry/dead-letter policy.

**`src/Shared.Npgsql`** — everything that touches PostgreSQL or EF Core:

- **`AddDatabaseConnection<TDbContext>`** — two overloads: one reads `DbConnection:*` configuration and authenticates to RDS Proxy with IAM tokens; the other takes a plain connection string and is what the tests use.
- **`InsertOutboxMessagesInterceptor`** — writes raised domain events into `outbox_messages` inside the same `SaveChanges` call.
- **`NpgsqlOutboxProcessorBase<TPublisher>`** — the claim → publish → record-outcome loop described in [The transactional outbox](#the-transactional-outbox).
- **`ApplyOutboxConfiguration`**, **`DatabaseClock`**, and **`ApplyMigrationsAsync`** — the outbox table mapping, a LINQ handle on PostgreSQL's `now()`, and startup migrations for the `Development` environment.

**`src/AWS.Aspire.ServiceDefaults`** — host wiring shared by the API and message-handler Lambdas. The name is historical: no Aspire AppHost uses it any more.

- **`AddServiceDefaults`** — OpenTelemetry tracing and metrics (ASP.NET Core, `HttpClient`, runtime, AWS SDK, AWS.Messaging, and Lambda instrumentation), service discovery, and standard `HttpClient` resilience.
- **`AddHttpApiLambdaDefaults`** — `AddServiceDefaults` plus endpoint discovery, Lambda HTTP API hosting, `GlobalExceptionHandler`, `ProblemDetails`, and OpenAPI.
- **`UseDefaultPipeline`** / **`MapDefaultEndpoints`** — exception handler, correlation-id middleware, and Serilog request logging; then the discovered endpoints, plus the OpenAPI document in `Development`.

**`src/Shared.AWS.CloudWatchLogs`** — a small helper used by the log-fetching scripts in `scripts/`.

---

## The bounded contexts

### Airports

The simplest context: airport reference data. Backed by **PostgreSQL** (`airports` schema).

The `Airport` aggregate raises `AirportCreated` on creation and `AirportUpdated` on every update; both leave through the outbox like every other event. Creating an airport whose IATA code already exists returns `409`.

- Publishes: `AirportCreated`, `AirportUpdated`
- Consumes: nothing

> Airports was originally backed by DynamoDB and published inline from its endpoints. It was moved onto PostgreSQL and the transactional outbox in July 2026, so all three services now share one persistence and publishing model.

### Aircraft

Owns aircraft, their weights, their seat maps, and their current location. Backed by **PostgreSQL** (`aircraft` schema).

Creating an aircraft reads `seat-layouts/{EquipmentCode}.json` from **S3** and expands the row-range definition into individual `Seat` entities, rejecting duplicate row/letter pairs. A missing layout yields a `404` rather than a `500`, and a duplicate tail number yields a `409`.

- Publishes: `AircraftCreated`
- Consumes: `FlightArrived`, `FlightMarkedAsEnRoute`, `FlightMarkedAsDelayedEnRoute`
- Owns an S3 bucket for seat layouts (`aircraft-bucket-{account}-{region}`)

### Flights

The richest context — flight scheduling, aircraft assignment, pricing, rescheduling, and the status lifecycle. Backed by **PostgreSQL** (`flights` schema).

It keeps **local read-model copies** of aircraft and airports, populated by the events it consumes. This is why scheduling a flight against a just-created airport or aircraft may `404` until the event propagates — and why the smoke tests retry.

Times are modelled with **NodaTime**: `LocalDateTime` for the scheduled wall-clock times, resolved to `ZonedDateTime`/`Instant` through the airport's IANA time zone.

- Publishes: `FlightScheduled`, `AircraftAssignedToFlight`, `FlightPricingAdjusted`, `FlightRescheduled`, `FlightCancelled`, `FlightDelayed`, `FlightArrived`, `FlightMarkedAsEnRoute`, `FlightMarkedAsDelayedEnRoute`
- Consumes: `AircraftCreated`, `AirportCreated`, `AirportUpdated`

---

## Event choreography

| Event                          | Published by | Consumed by | Effect on the consumer                      |
| ------------------------------ | ------------ | ----------- | ------------------------------------------- |
| `AirportCreated`               | Airports     | Flights     | Adds the airport to the local read model    |
| `AirportUpdated`               | Airports     | Flights     | Updates the local airport read model        |
| `AircraftCreated`              | Aircraft     | Flights     | Adds the aircraft to the local read model   |
| `FlightMarkedAsEnRoute`        | Flights      | Aircraft    | `aircraft.MarkAsEnRoute(destination)`       |
| `FlightMarkedAsDelayedEnRoute` | Flights      | Aircraft    | `aircraft.MarkAsEnRoute(destination)`       |
| `FlightArrived`                | Flights      | Aircraft    | `aircraft.MarkAsParked(arrivalAirportIcao)` |

Events published without a consumer today (`FlightScheduled`, `FlightCancelled`, `FlightDelayed`, `FlightRescheduled`, `FlightPricingAdjusted`, `AircraftAssignedToFlight`) still flow through SNS and exist as extension points for notification, pricing, or reporting consumers.

**Message envelope.** Handlers receive an SQS message whose body is the SNS envelope; the domain payload sits at `Message` → `data`. Handlers parse that path explicitly. An empty envelope, a payload that deserialises to `null`, or a reference to an entity the consumer does not hold is logged and skipped rather than retried. Anything that throws fails the invocation, and SQS redelivers the message up to three times before moving it to the queue's dead-letter queue.

Because outbox delivery is at-least-once, consumers must tolerate duplicates. The `AirportCreated` and `AircraftCreated` handlers, for example, skip rows that already exist.

---

## The transactional outbox

No service publishes from a request handler. Instead:

1. Domain models raise events into `Entity.DomainEvents` during a unit of work.
2. `InsertOutboxMessagesInterceptor` — an EF `SaveChangesInterceptor` registered by `AddDatabaseConnection` — serialises those events into `outbox_messages` **in the same transaction as the state change**, then clears them after save.
3. Each service's **Publisher Lambda** runs every minute. It claims due rows with a single statement — a `SELECT … FOR UPDATE SKIP LOCKED` CTE feeding an `UPDATE … RETURNING` that stamps a claim id and a claim expiry — and only then publishes them to SNS via the `AWS.Messaging` bus, recording each message's outcome on its own.

The shared loop lives in `NpgsqlOutboxProcessorBase<TPublisher>` (claiming, leasing, recording outcomes) and `OutboxProcessorBase<TPublisher>` (publishing, retries, dead-lettering). Each service's `OutboxProcessor` supplies only its schema-qualified claim SQL and its `s_publishers` registry.

`SKIP LOCKED` keeps two concurrent claims off the same rows; the `claimed_until_utc` stamp keeps concurrent invocations off each other's rows once the claiming statement has committed. Both the stamp and every check of it are computed by PostgreSQL's own `now()` (written in LINQ as `DatabaseClock.UtcNow()`), never by the Lambda: the lease is shared state, so the clock that grants and expires it has to be the one clock all the invocations agree on rather than the wall clock of whichever machine happened to claim the row. Claims expire after `OutboxConstants.ClaimDuration` (two minutes, comfortably clear of the publisher's 60-second Lambda timeout), so a message whose invocation dies mid-flight becomes eligible again rather than sticking forever.

Each outcome is written by a single `UPDATE` conditioned on both the claim id and an unexpired lease, so an invocation that overran its lease cannot overwrite state that another worker now owns. If that `UPDATE` matches no rows, or fails outright, the worker abandons the rest of its claim rather than publishing messages whose outcomes it may equally be unable to record. As a local optimisation, the loop also stops publishing once it is within `OutboxConstants.ClaimSafetyMargin` (10 seconds) of the lease expiring. Ownership itself is decided only in the database.

Publishing to SNS and marking a row processed can never be atomic, so delivery is at-least-once and consumers must be idempotent. Publishing **outside** the claim transaction bounds the blast radius: a failure costs at most the message in flight rather than every message already published in the same batch, and no row lock or open transaction is held across an SNS call.

**Retries and dead-lettering.** A failed publish is retried with exponential backoff — 30 seconds, doubling per attempt, capped at one hour — and dead-lettered after 5 attempts. Some failures are treated as **unrecoverable** and dead-lettered immediately: a message type with no registered publisher, a payload that is not valid JSON for its type, and a `NotSupportedException` from the publisher. A missing registration is a deployment bug, and retrying it would only delay the signal.

**Adding a new published event requires every step below.** Miss one and the message either dead-letters or never reaches a topic:

1. Raise it from the domain model via `AddDomainEvent`.
2. Register a publisher for it in that service's `OutboxProcessor.s_publishers` dictionary.
3. Register it in that Publisher Lambda's `AddAWSMessageBus` configuration (Flights keeps this in `AwsMessageBusInstaller.cs`).
4. Provision the SNS topic in `MessagingResources`, pass its ARN to the publisher as `SNS:<EventName>TopicArn`, and add it to the `PublisherLambda` construct's `Topics` so the function is granted publish rights.

---

## HTTP API reference

All routes are served under the shared API Gateway custom domain at `/api`.

### Airports

| Method | Route            | Body                       | Notes                                                 |
| ------ | ---------------- | -------------------------- | ----------------------------------------------------- |
| `GET`  | `/airports`      | —                          | List all airports                                     |
| `GET`  | `/airports/{id}` | —                          | Fetch one airport                                     |
| `POST` | `/airports`      | `CreateOrUpdateAirportDto` | Raises `AirportCreated`; `409` on duplicate IATA code |
| `PUT`  | `/airports/{id}` | `CreateOrUpdateAirportDto` | Raises `AirportUpdated`                               |

`CreateOrUpdateAirportDto`: `IcaoCode` (4 uppercase letters), `IataCode` (3 uppercase letters), `Name`, `TimeZoneId` (IANA, e.g. `Europe/Amsterdam`).

### Aircraft

| Method | Route            | Body                | Notes                                                                       |
| ------ | ---------------- | ------------------- | --------------------------------------------------------------------------- |
| `GET`  | `/aircraft`      | —                   | Paged list; filter by `parkedAt` **or** `enRouteTo` (`400` if both)         |
| `GET`  | `/aircraft/{id}` | —                   | Fetch one aircraft                                                          |
| `POST` | `/aircraft`      | `CreateAircraftDto` | Resolves the seat layout from S3; `404` if missing, `409` on duplicate tail |

`CreateAircraftDto`: `TailNumber`, `EquipmentCode`, `DryOperatingWeight`, `Status` (`Parked` \| `EnRoute`), `MaximumTakeoffWeight`, `ParkedAt?`, `EnRouteTo?`, `MaximumLandingWeight`, `MaximumZeroFuelWeight`, `MaximumFuelWeight`.

### Flights

| Method  | Route                    | Body                        | Notes                                                            |
| ------- | ------------------------ | --------------------------- | ---------------------------------------------------------------- |
| `GET`   | `/flights`               | —                           | Paged list; filter by `from` / `to` IATA code                    |
| `GET`   | `/flights/{id}`          | —                           | Fetch one flight                                                 |
| `POST`  | `/flights`               | `ScheduleFlightDto`         | `404` if the aircraft or an airport is not yet in the read model |
| `PATCH` | `/flights/{id}/status`   | `AdjustFlightStatusDto`     | Validated against the transition table                           |
| `PATCH` | `/flights/{id}/aircraft` | `AssignAircraftToFlightDto` | Reassign the operating aircraft                                  |
| `PATCH` | `/flights/{id}/pricing`  | `AdjustFlightPricingDto`    | Adjust economy/business fares                                    |
| `PATCH` | `/flights/{id}/schedule` | `RescheduleFlightDto`       | Change departure/arrival local times                             |

- `ScheduleFlightDto`: `AircraftId`, `DepartureAirportId`, `ArrivalAirportId`, `DepartureLocalTime`, `ArrivalLocalTime`, `EconomyPrice`, `BusinessPrice`, `FlightNumberIata`, `FlightNumberIcao`, `OperationType`, `SchedulingAmbiguityPolicy`.
- `RescheduleFlightDto`: `DepartureLocalTime`, `ArrivalLocalTime`, `SchedulingAmbiguityPolicy`.
- `AdjustFlightStatusDto`: `Status`. `AssignAircraftToFlightDto`: `AircraftId`. `AdjustFlightPricingDto`: `EconomyPrice`, `BusinessPrice`.

### Paging

`GET /aircraft` and `GET /flights` take `page` (default `1`) and `pageSize` (default `50`, clamped to `1`–`200`), and return an `AircraftListDto` / `FlightListDto`: `Items`, `Page`, `PageSize`, `TotalItems`, `HasNextPage`. A `page` past the end is clamped to the last page. Filter values are trimmed and upper-cased before matching. Flights are ordered by departure local time; aircraft by id.

Errors are returned as RFC-compliant `ProblemDetails` on `400`, `404`, `409`, and `500`.

---

## Domain rules worth knowing

**Flight status transitions** are enforced by `FlightStatusTransitions`; an illegal transition is rejected rather than silently applied. Self-transitions are disallowed.

```
Scheduled ──▶ EnRoute ──▶ Arrived
    │            │
    │            └──▶ DelayedEnRoute ──▶ EnRoute
    │                        └──────────▶ Arrived
    ├──▶ Delayed ──▶ DelayedEnRoute
    │        └─────▶ Cancelled
    └──▶ Cancelled

Arrived and Cancelled are terminal.
```

Each successful transition raises a matching event via `FlightStatusEventFactory`.

**Scheduling ambiguity.** Flights are scheduled in local wall-clock time, which is ambiguous or non-existent across DST boundaries. `SchedulingAmbiguityPolicy` — `ThrowWhenAmbiguous`, `PreferEarlier`, or `PreferLater` — is stored on the flight and drives the NodaTime `ZoneLocalMappingResolver` used whenever a local time is resolved to an instant. The policy is persisted rather than applied once at creation, so recomputing an instant later yields the same answer.

**Operation types.** `RevenuePassenger`, `NonRevenuePositioning`, `MaintenanceFerry`, `PermitToFly`.

**Aircraft location** is a small invariant: `MarkAsEnRoute` sets `EnRouteTo` and clears `ParkedAt`; `MarkAsParked` does the reverse. Both normalise the location code to trimmed uppercase, so an aircraft is never simultaneously parked and en route.

**Seat layouts** are declared as row ranges rather than individual seats, with an optional `EveryNthRowOnly` for staggered cabins. [`Resources/Layouts/B78X.json`](Resources/Layouts/B78X.json) staggers its business cabin across two overlapping ranges:

```json
{
    "EquipmentType": "B78X",
    "BusinessRows": {
        "1-17": { "Seats": ["A", "K"], "SeatType": "Business", "EveryNthRowOnly": 2 },
        "2-18": { "Seats": ["D", "F"], "SeatType": "Business", "EveryNthRowOnly": 2 }
    },
    "EconomyRows": {
        "19-49": { "Seats": ["A", "B", "C", "D", "E", "F", "G", "H", "J"], "SeatType": "Economy" },
        "50": { "Seats": ["A", "B", "C", "D", "F", "G", "H", "J"], "SeatType": "Economy" },
        "51-52": { "Seats": ["A", "B", "D", "E", "F", "G", "J"], "SeatType": "Economy" }
    }
}
```

---

## Repository layout

```
├── src/
│   ├── Shared/                                  # Contracts, endpoints, guards, error handling, outbox primitives
│   ├── Shared.Npgsql/                           # PostgreSQL/EF Core wiring, outbox interceptor and drain loop
│   ├── Shared.AWS.CloudWatchLogs/               # Log-fetching helper used by scripts/
│   ├── AWS.Aspire.ServiceDefaults/              # OpenTelemetry + HTTP API Lambda host defaults
│   ├── Airports.Core|Infrastructure|Api.Lambda/
│   ├── Airports.Publisher.Lambda|Migrations.Lambda/
│   ├── Aircraft.Core|Infrastructure|Api.Lambda/
│   ├── Aircraft.Publisher.Lambda|Migrations.Lambda/
│   ├── Aircraft.Api.Lambda.MessageHandlers.*/   # FlightArrived, FlightMarkedAsEnRoute, FlightMarkedAsDelayedEnRoute
│   ├── Flights.Core|Infrastructure|Api.Lambda/
│   ├── Flights.Publisher.Lambda|Migrations.Lambda/
│   └── Flights.Api.Lambda.MessageHandlers.*/    # AircraftCreated, AirportCreated, AirportUpdated
├── tests/
│   ├── {Airports,Aircraft,Flights}.Api.Lambda.FunctionalTests/
│   ├── {Airports,Aircraft,Flights}.Infrastructure.IntegrationTests/
│   ├── Shared.Npgsql.UnitTests/
│   └── SmokeTests/                              # End-to-end against a live deployment
├── Deployment/                                  # AWS CDK app (C#)
├── docker/                                      # One dockerfile per Lambda (15 in total)
├── scripts/                                     # File-based C# apps: coverage report, CloudWatch log fetchers
├── Resources/Layouts/                           # Seat-layout definitions
├── Directory.Build.props                        # Analyzers, warnings-as-errors, net10.0
├── Directory.Packages.props                     # Central package version management
├── global.json                                  # Selects Microsoft.Testing.Platform as the test runner
└── EmbassyAirlines.slnx                         # XML solution format
```

---

## Getting started

### Prerequisites

- **.NET 10 SDK**
- **Docker** — required for the functional and integration tests (Testcontainers) and for building Lambda images
- **Node.js** — for `npx prettier`
- **ReportGenerator** (`dotnet tool install -g dotnet-reportgenerator-globaltool`) — only for the coverage report
- **AWS CLI + credentials** — only for deployment, smoke tests, and the log scripts
- **AWS CDK CLI** — only for deployment

### Clone and build

```bash
git clone <repository-url>
cd EmbassyAirlines
dotnet build
dotnet test          # requires Docker to be running
```

### Running locally

There is currently **no local orchestration** for the services. The .NET Aspire AppHost that used to run Airports against DynamoDB Local was removed when Airports moved to PostgreSQL.

The test suites are the local harness. The functional tests boot each real API in-process against PostgreSQL (and, for Aircraft, LocalStack S3) containers, and the integration tests run the outbox against a real PostgreSQL. Running an API project directly with `dotnet run` is not supported as-is: under `Development` it expects its host to register the `DbContext`, which only the test factories do.

---

## Build, test, and tooling

The solution file is `EmbassyAirlines.slnx` — the newer XML format, which most `dotnet` commands pick up automatically from the repo root. Tests run on **Microsoft.Testing.Platform** (selected in `global.json`) using xUnit v3.

```bash
# Build (warnings are errors)
dotnet build

# Format: CI runs the verify form; run the plain form to auto-fix before committing
dotnet format --verify-no-changes
dotnet format

# Prettier gates non-C# files (JSON, YAML, Markdown) with default config
npx prettier --check .
npx prettier --write .

# Tests
dotnet test
dotnet test --project tests/Aircraft.Api.Lambda.FunctionalTests
dotnet test --filter "FullyQualifiedName~AircraftTests.CreateAircraft_ShouldReturnCreated"

# Coverage: runs the tests with coverlet, then writes an HTML report to Reports/CoverageReport/
dotnet run scripts/Coverage.cs
```

**Docker must be running for the functional and integration tests** — they start PostgreSQL (and LocalStack) containers via Testcontainers.

### CI

`.github/workflows/main.yml` runs on push to `main` as two parallel jobs:

- **Prettier** — `npx prettier --check .`
- **DotNet** — `dotnet format --verify-no-changes`, then `dotnet build -c Release`, then `dotnet test`

Both must pass. Dependabot checks daily for updates to NuGet packages, the base images in `docker/`, and the GitHub Actions used by the workflow.

---

## Code-quality enforcement

`Directory.Build.props` applies to every project:

- `TreatWarningsAsErrors=true` and `CodeAnalysisTreatWarningsAsErrors=true`
- `AnalysisMode=All` at `latest` analysis level
- `EnforceCodeStyleInBuild=true`
- `Nullable` and `ImplicitUsings` enabled
- SonarAnalyzer.CSharp on every project

**A build fails on any analyzer or style violation.** `.editorconfig` promotes several conventions to _errors_ — no `this.` qualification, predefined type keywords over BCL names, and others.

When a rule is genuinely not applicable, suppress it in `.editorconfig` alongside the existing `dotnet_diagnostic.*.severity = none` entries rather than inline. Inline suppressions hide the decision from everyone who is not reading that exact file.

NuGet versions are centrally managed in `Directory.Packages.props` — **reference packages without a `Version` attribute**.

---

## Configuration

Each service reads environment variables under a **service-specific prefix**: `AIRPORTS_`, `AIRCRAFT_`, `FLIGHTS_`. Configuration keys nest with `__`, so `DbConnection:Host` becomes `AIRCRAFT_DbConnection__Host`. CDK sets all of these; `appsettings.json` holds only logging and host settings.

| Key                                          | Applies to                | Purpose                                       |
| -------------------------------------------- | ------------------------- | --------------------------------------------- |
| `DbConnection:{Host,Database,Username,Port}` | Every Lambda in a service | RDS Proxy connection (no password — IAM auth) |
| `SNS:<EventName>TopicArn`                    | Publisher Lambdas         | Target topic per event type                   |
| `S3:BucketName`                              | Aircraft API              | Seat-layout bucket                            |

Outbox tuning is **not** configurable at runtime. It lives in `OutboxConstants` in `src/Shared`:

| Constant            | Value      | Meaning                                               |
| ------------------- | ---------- | ----------------------------------------------------- |
| `BatchSize`         | `100`      | Rows claimed per publisher invocation                 |
| `MaxRetryAttempts`  | `5`        | Attempts before a message is dead-lettered            |
| `BaseRetryDelay`    | 30 seconds | First backoff delay; doubles per attempt              |
| `MaxRetryDelay`     | 1 hour     | Backoff ceiling                                       |
| `ClaimDuration`     | 2 minutes  | Lease length stamped on claimed rows                  |
| `ClaimSafetyMargin` | 10 seconds | Stop publishing once this close to the lease expiring |

---

## Database and migrations

`AddDatabaseConnection` builds an Npgsql data source that authenticates to **RDS Proxy** using IAM tokens via `RDSAuthTokenGenerator`. **There is no static password**, and `SslMode.Require` is enforced. Lambdas never reach PostgreSQL directly — always through the proxy.

All three services share one database. Each uses a dedicated schema (`airports`, `aircraft`, `flights`) with its own `__EFMigrationsHistory` table and snake_case naming via `UseSnakeCaseNamingConvention`. Flights also enables NodaTime type mapping.

**How migrations are applied:**

- **Deployed:** each service's `X.Migrations.Lambda` is the handler of a CloudFormation custom resource (`DatabaseMigrationLambda` in CDK). The custom resource's properties include the migration image's asset hash, so CloudFormation re-invokes it whenever that image changes, which includes every new migration. Every other Lambda in the service declares a dependency on it, so the schema is migrated before the code that needs it is updated. `Delete` events are a no-op: removing the custom resource never rolls a schema back.
- **`Development` environment:** the API applies migrations at startup via `ApplyMigrationsAsync`. This is the path the functional tests exercise.

To add a migration, use the `DesignTimeDbContextFactory` in each `X.Infrastructure` project — it supplies a throwaway local connection string, so no live database is needed. `Microsoft.EntityFrameworkCore.Design` is referenced by the `X.Api.Lambda` projects, so pass one as the startup project:

```bash
dotnet ef migrations add <Name> --project src/Aircraft.Infrastructure --startup-project src/Aircraft.Api.Lambda
```

---

## Deployment

`Deployment/` is a C# AWS CDK app (`cdk.json` → `dotnet run --project Deployment/Deployment.csproj`). The stack takes its account and region from `CDK_DEFAULT_ACCOUNT` / `CDK_DEFAULT_REGION`, which the CDK CLI resolves from your AWS profile. See [`Deployment/README.md`](Deployment/README.md) for the full breakdown.

```bash
cdk synth
cdk deploy
```

`EmbassyAirlinesStack` composes:

- **Networking** — a VPC across 2 AZs with **no NAT gateways**; every Lambda runs in private isolated subnets. An S3 gateway endpoint and SNS and SQS interface endpoints let Lambdas reach those services without internet egress.
- **`SharedInfra`** — imports the Route 53 hosted zone for `embassyairlines.com`, provisions a DNS-validated ACM certificate, and configures an API Gateway HTTP API custom domain mapped at the `/api` base path. Every service adds routes onto this one API.
- **`MessagingResources`** — the twelve SNS topics that form the communication backbone.
- **`RdsResources`** — one PostgreSQL 18 instance (`t4g.micro`, single-AZ) fronted by an RDS Proxy with IAM auth and Secrets Manager-generated credentials.
- **The three service constructs** — `AirportsService`, `AircraftService`, `FlightsService`.

Reusable Lambda constructs live in `Deployment/Lambdas/`:

| Construct                 | Provisions                                                                                         |
| ------------------------- | -------------------------------------------------------------------------------------------------- |
| `HttpDockerLambda`        | Docker Lambda + API Gateway route (`ANY /{service}`) + RDS Proxy access (30 s timeout)             |
| `EventHandlerLambda`      | Docker Lambda + SQS queue + DLQ (max 3 receives) + SNS subscription + event source (batch size 10) |
| `PublisherLambda`         | Docker Lambda on a one-minute EventBridge schedule + SNS publish grants (60 s timeout)             |
| `DatabaseMigrationLambda` | Docker Lambda + custom-resource provider that runs migrations on deploy (10 min timeout)           |

Every Lambda is a Docker image built from a dockerfile in `docker/`, has 1,536 MB of memory, gets its own security group with access to the RDS Proxy, and has X-Ray active tracing enabled.

---

## Observability

- **Structured logging** via Serilog. The API Lambdas write compact JSON to the console (and so to CloudWatch), with a correlation id attached by `RequestContextLoggingMiddleware` (`X-Correlation-Id`, falling back to the trace id) so a request can be followed across services.
- **Tracing.** Every Lambda has **AWS X-Ray active tracing** enabled in CDK, which is what produces the traces visible in X-Ray today. Separately, `AddServiceDefaults` wires up OpenTelemetry instrumentation, and message handlers wrap their invocation in `AWSLambdaWrapper.TraceAsync` and tag spans with domain identifiers (`flight.id`, `airport.icao_code`, …). No OpenTelemetry exporter is registered, though, so those in-process spans and metrics are not currently shipped anywhere — see [Deliberate trade-offs](#deliberate-trade-offs).
- **Log retrieval.** `scripts/Get*Logs.cs` are file-based C# apps (using the `#:project` directive) that print recent CloudWatch logs for each service's **API** Lambda. They require AWS credentials:

    ```bash
    dotnet run scripts/GetAircraftLogs.cs
    dotnet run scripts/GetAirportsLogs.cs
    dotnet run scripts/GetFlightsLogs.cs
    ```

---

## Testing strategy

The automated suites run on **xUnit v3** under Microsoft.Testing.Platform. The functional and unit tests assert with **AwesomeAssertions**, the Apache-licensed fork of FluentAssertions.

### Functional tests

`tests/*.Api.Lambda.FunctionalTests` use `WebApplicationFactory<Program>` to boot each real API in-process. `WebApplicationFactory` runs the app under the `Development` environment, so the API skips the RDS Proxy/IAM `AddDatabaseConnection` path and applies migrations at startup. `FunctionalTestWebAppFactory` registers the `DbContext` against a **Testcontainers PostgreSQL** (`postgres:18`). For Aircraft it also starts **LocalStack** and seeds a seat layout into S3. `BaseFunctionalTest` supplies the shared `HttpClient` and `ProblemDetails` assertion helpers.

These are genuine HTTP tests against a real database — not mocked unit tests.

### Integration tests

`tests/*.Infrastructure.IntegrationTests` exercise the transactional outbox against a real PostgreSQL container. The Aircraft suite is the thorough one: it covers the interceptor writing outbox rows in the same transaction, concurrent processors never publishing the same message, expired claims being reclaimed, a worker that lost its lease being unable to overwrite the new owner's outcome, exponential backoff, and dead-lettering of unknown types, invalid JSON, and unrecoverable failures. The Airports and Flights suites currently cover the publish-and-mark-processed path.

### Unit tests

`tests/Shared.Npgsql.UnitTests` checks that `AddDatabaseConnection` registers its services from configuration.

### Smoke tests

`tests/SmokeTests` is a console app that exercises the full workflow against a **live** deployment: probe each service → create airports → upload a seat layout to S3 → create an aircraft → schedule a flight. See [`tests/SmokeTests/README.md`](tests/SmokeTests/README.md).

```bash
dotnet run --project tests/SmokeTests -- https://embassyairlines.com/api/
```

It requires AWS credentials (it uploads a seat layout to S3 and resolves account/region via the AWS CLI). Because the backend is eventually consistent — and every event now waits for a publisher run of up to a minute — the flight-scheduling step **retries on `404`** once a second for up to 180 attempts, to absorb the delay before the Flights context learns about the new airports and aircraft.

---

## Deliberate trade-offs

Choices made for a demonstration project that would differ in production:

- **All three services share one PostgreSQL instance**, logically separated by schema. Separate instances per service would be the production answer, but would multiply infrastructure cost for no additional architectural demonstration. The schema separation means splitting them later is straightforward.
- **The database is sized and protected for a demo**: a single-AZ `t4g.micro`, with deletion protection off and a `DESTROY` removal policy.
- **Outbox latency is up to a minute.** Publishers poll on a one-minute schedule rather than being triggered by writes, which is simple and cheap but puts a floor under end-to-end propagation time.
- **A single deployed environment**, not a full dev/staging/prod pipeline. CI builds and tests but does not deploy.
- **No local orchestration.** Local runs go through the functional and integration tests rather than an emulated stack.
- **No NAT gateways.** Lambdas reach AWS services through VPC endpoints only. This is a cost and security win, but means no outbound internet access from inside the VPC.
- **OpenTelemetry is instrumented but not exported.** The Lambdas are container images, so OTel/ADOT Lambda layers cannot be attached, and the isolated VPC has no route to an external OTLP collector. X-Ray active tracing covers the gap. Shipping the custom spans would need a collector reachable from the VPC plus an exporter registered in `AddServiceDefaults`.

---

## Extending the system

**Adding an HTTP endpoint.** Create a class implementing `IEndpoint` in the service's `Endpoints/` folder. Reflection-based discovery registers it — no manual wiring. Follow the existing pattern: validate with FluentValidation, return `ErrorOr`-based errors mapped through `ErrorHandlingHelper`, and declare the response shape with `.Produces<T>()` / `.ProducesProblem()`.

**Adding a published event.** Add the event record to `Shared.Contracts`, then follow every step in [The transactional outbox](#the-transactional-outbox): raise it from the domain model, register it in `OutboxProcessor.s_publishers` and the Publisher Lambda's `AddAWSMessageBus`, and provision and grant the SNS topic in CDK.

**Adding a consumer.** Create a new `X.Api.Lambda.MessageHandlers.<EventName>` project with its own `Function` class and `HostApplicationBuilder`, add it to `EmbassyAirlines.slnx`, add a matching dockerfile in `docker/`, and wire an `EventHandlerLambda` construct in the service's CDK construct to create the queue, DLQ, and SNS subscription. Make the dependency on the service's migration explicit, as the existing handlers do. Remember that the payload sits at `Message` → `data` in the SNS-over-SQS envelope, and that delivery is at-least-once.

**Changing a schema.** Change the model in `X.Core` / `X.Infrastructure`, add a migration with `dotnet ef` (see [Database and migrations](#database-and-migrations)), and deploy. The Migrations Lambda applies it before the dependent Lambdas are updated.

**Changing a contract.** `Shared.Contracts` records are the wire format between independently deployed services, and serialised events also sit in `outbox_messages` waiting to be published. A breaking change needs a rollout plan — additive-optional fields first, or a versioned event — because producer and consumer will be running different builds during any deployment.
