# Embassy Airlines

A demonstration event-driven microservices backend built on **.NET 10** and **AWS**. It models an airline operations domain across three bounded contexts — **Airports**, **Aircraft**, and **Flights** — which communicate asynchronously over SNS topics and SQS queues and are **eventually consistent**.

Every runtime component ships as a Docker-image AWS Lambda. Infrastructure is provisioned with AWS CDK, also written in C#. The target region is `eu-west-2`.

> This is a portfolio/demonstration project. Some choices (a single shared PostgreSQL instance, a single staging environment) trade production-grade isolation for a sane infrastructure bill — these are called out in [Deliberate trade-offs](#deliberate-trade-offs).

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

Nothing in step 5 is a synchronous call. The Flights context never invokes the Aircraft context; it publishes an event and moves on.

---

## Architecture

```
                                   API Gateway (custom domain, /api)
                                                │
                 ┌──────────────────────────────┼──────────────────────────────┐
                 │                              │                              │
          ┌──────▼──────┐                ┌──────▼──────┐                ┌──────▼──────┐
          │  Airports   │                │  Aircraft   │                │   Flights   │
          │  API Lambda │                │  API Lambda │                │  API Lambda │
          └──────┬──────┘                └──────┬──────┘                └──────┬──────┘
                 │                              │                              │
          ┌──────▼──────┐                ┌──────▼──────┐                ┌──────▼──────┐
          │  DynamoDB   │                │ PostgreSQL  │                │ PostgreSQL  │
          │  (airports) │                │  (aircraft  │                │  (flights   │
          │             │                │   schema)   │                │   schema)   │
          └──────┬──────┘                └──────┬──────┘                └──────┬──────┘
                 │                              │                              │
          publishes inline            outbox → Publisher Lambda        outbox → Publisher Lambda
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

### Per-service layering

Aircraft and Flights are each split into layered projects. Airports is a deliberately slimmer variant with no `Core`/`Infrastructure` split.

| Project                                    | Responsibility                                                                                                                                                                           |
| ------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `X.Core`                                   | Domain aggregates with real behaviour (`Aircraft.Create`, `flight.AdjustStatus`). Entities extend `Entity` and raise domain events via `AddDomainEvent`. No infrastructure dependencies. |
| `X.Infrastructure`                         | EF Core `ApplicationDbContext`, entity configurations, migrations, the outbox implementation, and `AddDatabaseConnection` DI wiring.                                                     |
| `X.Api.Lambda`                             | ASP.NET Core Minimal API hosted in Lambda via `AddAWSLambdaHosting(LambdaEventSource.HttpApi)`. HTTP endpoints only.                                                                     |
| `X.Publisher.Lambda`                       | The outbox drainer, invoked on a one-minute schedule.                                                                                                                                    |
| `X.Api.Lambda.MessageHandlers.<EventName>` | One Lambda project **per consumed event**. Each is an SQS-triggered `Function` class owning its own `HostApplicationBuilder`.                                                            |

Splitting message handlers one-per-event means each consumer scales, fails, retries, and dead-letters independently — a poison message on `FlightArrived` cannot stall `FlightMarkedAsEnRoute`.

### Shared library

`src/Shared` holds the cross-cutting contracts and infrastructure every service depends on. See [`src/Shared/README.md`](src/Shared/README.md) for the full tour. The pieces you touch most often:

- **`Shared.Contracts`** — immutable record DTOs (`AircraftDto`, `ScheduleFlightDto`, …) and integration events (`AircraftCreatedEvent`, `FlightArrivedEvent`, …) implementing `IDomainEvent` / `IFlightStatusManagementEvent`. **These records are the wire contract between services** — changing one affects both producer and consumer, and they deploy independently.
- **`IEndpoint`** — every Minimal API endpoint is a class implementing `MapEndpoint`. `AddEndpoints(assembly)` + `MapEndpoints()` discover and register them by reflection, so a new endpoint needs no manual registration.
- **`Ensure`** — guard-clause helpers (`NotNullOrEmpty`, `GreaterThanZero`, …) using `CallerArgumentExpression`, so the failing parameter name comes for free. Prefer these over hand-written argument checks.
- **Error handling** — endpoints return `ErrorOr`-based results mapped through `ErrorHandlingHelper.GetProblemDetails`; `GlobalExceptionHandler` catches the rest. Everything surfaces as RFC-compliant `ProblemDetails`. Follow the existing endpoint pattern rather than throwing.
- **`RequestContextLoggingMiddleware`** — enriches Serilog logs with a correlation id from the `X-Correlation-Id` header, falling back to the trace id.

---

## The bounded contexts

### Airports

The simplest context. Backed by a **DynamoDB** table, with no `Core`/`Infrastructure` split and no outbox — it publishes `AirportCreated` / `AirportUpdated` **inline** from the endpoint via `IMessagePublisher`.

This asymmetry is intentional: the Airports write is a single-item DynamoDB put with no surrounding relational transaction, so there is no atomicity gap for an outbox to close. Aircraft and Flights write multiple rows in one transaction and genuinely need one. See [Deliberate trade-offs](#deliberate-trade-offs).

### Aircraft

Owns aircraft, their weights, their seat maps, and their current location. Backed by **PostgreSQL** (`aircraft` schema).

Creating an aircraft reads `seat-layouts/{EquipmentCode}.json` from **S3** and expands the row-range definition into individual `Seat` entities, rejecting duplicate row/letter pairs. A missing layout yields a `404` rather than a `500`.

- Publishes: `AircraftCreated`
- Consumes: `FlightArrived`, `FlightMarkedAsEnRoute`, `FlightMarkedAsDelayedEnRoute`
- Owns an S3 bucket for seat layouts

### Flights

The richest context — flight scheduling, aircraft assignment, pricing, rescheduling, and the status lifecycle. Backed by **PostgreSQL** (`flights` schema).

It keeps **local read-model copies** of aircraft and airports, populated by the events it consumes. This is why scheduling a flight against a just-created airport may `404` until the event propagates — and why the smoke tests retry.

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

**Message envelope.** Handlers receive an SQS message whose body is the SNS envelope; the domain payload sits at `Message` → `data`. Handlers parse that path explicitly and log-and-return on malformed or unresolvable payloads rather than throwing, so a bad message does not spin on the SQS retry cycle.

---

## The transactional outbox

Aircraft and Flights never publish inline. Instead:

1. Domain models raise events into `Entity.DomainEvents` during a transaction.
2. `InsertOutboxMessagesInterceptor` — an EF `SaveChangesInterceptor` registered in `AddDatabaseConnection` — serialises those events into `outbox_messages` **in the same transaction as the state change**, then clears them after save.
3. The **Publisher Lambda** runs every minute, selects due rows with `FOR UPDATE SKIP LOCKED`, and publishes to SNS via the `AWS.Messaging` bus.

`OutboxProcessor` handles retries with exponential backoff and dead-letters messages that exhaust `Outbox:MaxRetryAttempts`. `SKIP LOCKED` means concurrent publisher invocations can drain the same table without contending or double-publishing.

**Adding a new published event requires three steps.** Miss any one and the message dead-letters:

1. Raise it from the domain model via `AddDomainEvent`.
2. Register a publisher for it in that service's `OutboxProcessor.s_publishers` dictionary.
3. Register it in that Publisher Lambda's `AddAWSMessageBus` configuration (and provision the SNS topic in CDK).

An unregistered message type is treated as **unrecoverable** and dead-lettered immediately rather than retried — a missing registration is a deployment bug, and retrying it 5 times would only delay the signal.

---

## HTTP API reference

All routes are served under the shared API Gateway custom domain at `/api`.

### Airports

| Method | Route            | Body                       | Notes                      |
| ------ | ---------------- | -------------------------- | -------------------------- |
| `GET`  | `/airports`      | —                          | List airports              |
| `GET`  | `/airports/{id}` | —                          | Fetch one airport          |
| `POST` | `/airports`      | `CreateOrUpdateAirportDto` | Publishes `AirportCreated` |
| `PUT`  | `/airports/{id}` | `CreateOrUpdateAirportDto` | Publishes `AirportUpdated` |

`CreateOrUpdateAirportDto`: `IcaoCode`, `IataCode`, `Name`, `TimeZoneId` (IANA, e.g. `Europe/Amsterdam`).

### Aircraft

| Method | Route            | Body                | Notes                                                            |
| ------ | ---------------- | ------------------- | ---------------------------------------------------------------- |
| `GET`  | `/aircraft`      | —                   | List aircraft                                                    |
| `GET`  | `/aircraft/{id}` | —                   | Fetch one aircraft                                               |
| `POST` | `/aircraft`      | `CreateAircraftDto` | Resolves the seat layout from S3; `409` on duplicate tail number |

`CreateAircraftDto`: `TailNumber`, `EquipmentCode`, `DryOperatingWeight`, `Status` (`Parked` \| `EnRoute`), `MaximumTakeoffWeight`, `ParkedAt?`, `EnRouteTo?`, `MaximumLandingWeight`, `MaximumZeroFuelWeight`, `MaximumFuelWeight`.

### Flights

| Method  | Route                    | Body                        | Notes                                  |
| ------- | ------------------------ | --------------------------- | -------------------------------------- |
| `GET`   | `/flights`               | —                           | List flights                           |
| `GET`   | `/flights/{id}`          | —                           | Fetch one flight                       |
| `POST`  | `/flights`               | `ScheduleFlightDto`         | Schedule a flight                      |
| `PATCH` | `/flights/{id}/status`   | `AdjustFlightStatusDto`     | Validated against the transition table |
| `PATCH` | `/flights/{id}/aircraft` | `AssignAircraftToFlightDto` | Reassign the operating aircraft        |
| `PATCH` | `/flights/{id}/pricing`  | `AdjustFlightPricingDto`    | Adjust economy/business fares          |
| `PATCH` | `/flights/{id}/schedule` | `RescheduleFlightDto`       | Change departure/arrival local times   |

`ScheduleFlightDto`: `AircraftId`, `DepartureAirportId`, `ArrivalAirportId`, `DepartureLocalTime`, `ArrivalLocalTime`, `EconomyPrice`, `BusinessPrice`, `FlightNumberIata`, `FlightNumberIcao`, `OperationType`, `SchedulingAmbiguityPolicy`.

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

**Seat layouts** are declared as row ranges rather than individual seats, with an optional `EveryNthRowOnly` for staggered business cabins. See [`Resources/Layouts/B78X.json`](Resources/Layouts/B78X.json):

```json
{
    "EquipmentType": "B78X",
    "BusinessRows": {
        "1-17": { "Seats": ["A", "K"], "SeatType": "Business", "EveryNthRowOnly": 2 }
    },
    "EconomyRows": {
        "19-49": { "Seats": ["A", "B", "C", "D", "E", "F", "G", "H", "J"], "SeatType": "Economy" }
    }
}
```

---

## Repository layout

```
├── src/
│   ├── Shared/                                  # Contracts, endpoints, guards, error handling
│   ├── Shared.AWS.CloudWatchLogs/               # Log-fetching helper used by scripts/
│   ├── Airports.Api.Lambda/                     # DynamoDB-backed, no Core/Infrastructure split
│   ├── Aircraft.Core|Infrastructure|Api.Lambda/
│   ├── Aircraft.Publisher.Lambda/
│   ├── Aircraft.Api.Lambda.MessageHandlers.*/   # FlightArrived, FlightMarkedAsEnRoute, …
│   ├── Flights.Core|Infrastructure|Api.Lambda/
│   ├── Flights.Publisher.Lambda/
│   ├── Flights.Api.Lambda.MessageHandlers.*/    # AircraftCreated, AirportCreated, AirportUpdated
│   └── AWS.Aspire.AppHost|ServiceDefaults/      # Local orchestration
├── tests/
│   ├── {Airports,Aircraft,Flights}.Api.Lambda.FunctionalTests/
│   └── SmokeTests/                              # End-to-end against a live deployment
├── Deployment/                                  # AWS CDK app (C#)
├── docker/                                      # One dockerfile per Lambda
├── scripts/                                     # File-based C# CloudWatch log fetchers
├── Resources/Layouts/                           # Seat-layout definitions
├── Directory.Build.props                        # Analyzers, warnings-as-errors, net10.0
├── Directory.Packages.props                     # Central package version management
└── EmbassyAirlines.slnx                         # XML solution format
```

---

## Getting started

### Prerequisites

- **.NET 10 SDK**
- **Docker** — required for the functional tests (Testcontainers) and for building Lambda images
- **Node.js** — for `npx prettier`
- **AWS CLI + credentials** — only for deployment, smoke tests, and the log scripts
- **AWS CDK CLI** — only for deployment

### Clone and build

```bash
git clone <repository-url>
cd EmbassyAirlines
dotnet build
dotnet test          # requires Docker to be running
```

### Run locally

`src/AWS.Aspire.AppHost` is a .NET Aspire host that runs services against emulators — DynamoDB Local, the AWS Lambda service emulator, and an API Gateway emulator on port 3000.

```bash
dotnet run --project src/AWS.Aspire.AppHost
```

> **Note:** the AppHost currently wires up **only the Airports service**. Aircraft and Flights are exercised locally through their functional tests, which spin up real PostgreSQL and LocalStack containers.

---

## Build, test, and tooling

The solution file is `EmbassyAirlines.slnx` — the newer XML format, which most `dotnet` commands pick up automatically from the repo root.

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
dotnet test tests/Aircraft.Api.Lambda.FunctionalTests
dotnet test --filter "FullyQualifiedName~AircraftTests.CreateAircraft_ShouldReturnCreated"
dotnet test --settings coverlet.runsettings          # with coverage
```

**Docker must be running for the functional tests** — they start PostgreSQL and LocalStack containers via Testcontainers.

### CI

`.github/workflows/main.yml` runs on push to `main` and enforces, in order:

1. `npx prettier --check .`
2. `dotnet format --verify-no-changes`
3. `dotnet build -c Release`
4. `dotnet test`

All four must pass.

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

Each service reads environment variables under a **service-specific prefix**: `AIRCRAFT_`, `FLIGHTS_`, `AIRPORTS_`. Configuration keys nest with `__`, so `DbConnection:Host` becomes `AIRCRAFT_DbConnection__Host`.

| Key                                          | Applies to        | Purpose                                       |
| -------------------------------------------- | ----------------- | --------------------------------------------- |
| `DbConnection:{Host,Database,Username,Port}` | Aircraft, Flights | RDS Proxy connection (no password — IAM auth) |
| `SNS:<EventName>TopicArn`                    | Publishers        | Target topic per event type                   |
| `S3:BucketName`                              | Aircraft          | Seat-layout bucket                            |
| `Outbox:BatchSize`                           | Publishers        | Rows per drain (default `100`)                |
| `Outbox:MaxRetryAttempts`                    | Publishers        | Before dead-lettering (default `5`)           |
| `Outbox:BaseRetryDelaySeconds`               | Publishers        | Backoff base (default `30`)                   |
| `Outbox:MaxRetryDelaySeconds`                | Publishers        | Backoff ceiling (default `3600`)              |

---

## Database and migrations

`AddDatabaseConnection` builds an Npgsql data source that authenticates to **RDS Proxy** using IAM tokens via `RDSAuthTokenGenerator`. **There is no static password**, and `SslMode.Require` is enforced. Lambdas never reach PostgreSQL directly — always through the proxy.

Each service uses a dedicated schema (`aircraft`, `flights`) with snake_case naming via `UseSnakeCaseNamingConvention`. Migrations are applied automatically at API startup through `ApplyMigrationsAsync` (`Database.MigrateAsync()`).

To add a migration, use the `DesignTimeDbContextFactory` in each `X.Infrastructure` project — it supplies a throwaway local connection string, so no live database or startup project is needed:

```bash
dotnet ef migrations add <Name> --project src/Aircraft.Infrastructure
```

---

## Deployment

`Deployment/` is a C# AWS CDK app (`cdk.json` → `dotnet run --project Deployment/Deployment.csproj`). See [`Deployment/README.md`](Deployment/README.md) for the full breakdown.

```bash
cdk synth
cdk deploy
```

`EmbassyAirlinesStack` composes:

- **Networking** — a VPC across 2 AZs with **no NAT gateways** and private isolated subnets. VPC endpoints for S3, SNS, SQS, and DynamoDB let Lambdas reach AWS services without internet egress.
- **`SharedInfra`** — imports the Route 53 hosted zone, provisions a DNS-validated ACM certificate, and configures an API Gateway custom domain mapped at the `/api` base path. Every service adds routes onto this one API.
- **`MessagingResources`** — the SNS topics that form the communication backbone.
- **Shared RDS + RDS Proxy** with IAM auth and generated Secrets Manager credentials.
- **The three service constructs.**

Reusable Lambda constructs live in `Deployment/Lambdas/`:

| Construct            | Provisions                                                                      |
| -------------------- | ------------------------------------------------------------------------------- |
| `HttpDockerLambda`   | Docker-image Lambda + API Gateway route + DB access + security group            |
| `EventHandlerLambda` | Docker Lambda + SQS queue + dead-letter queue + SNS subscription + event source |
| `PublisherLambda`    | Scheduled (1-minute) outbox drainer                                             |

Every Lambda is a Docker image built from a dockerfile in `docker/`.

---

## Observability

- **Structured logging** via Serilog, with a correlation id attached by `RequestContextLoggingMiddleware` (`X-Correlation-Id`, falling back to the trace id) so a request can be followed across services.
- **Distributed tracing** via OpenTelemetry. Message handlers wrap their invocation in `AWSLambdaWrapper.TraceAsync` and tag spans with domain identifiers (`flight.id`, `flight.aircraft_id`, `flight.arrival_airport_icao_code`), which makes an async hop traceable end to end.
- **Log retrieval.** `scripts/*.cs` are file-based C# apps (using the `#:project` directive) that fetch recent CloudWatch logs for a given Lambda. They require AWS credentials:

    ```bash
    dotnet run scripts/GetAircraftLogs.cs
    dotnet run scripts/GetAirportsLogs.cs
    dotnet run scripts/GetFlightsLogs.cs
    ```

---

## Testing strategy

### Functional tests

`tests/*.FunctionalTests` use `WebApplicationFactory<Program>` with **xUnit v3** and **FluentAssertions** (pinned to `7.0.0` — the last MIT-licensed release).

`FunctionalTestWebAppFactory` boots the real application under the `FunctionalTests` environment, which skips the RDS/IAM `AddDatabaseConnection` path and substitutes **Testcontainers PostgreSQL** plus **LocalStack** for AWS services. `BaseFunctionalTest` supplies the shared `HttpClient` and `ProblemDetails` assertion helpers.

These are genuine end-to-end HTTP tests against a real database — not mocked unit tests.

### Smoke tests

`tests/SmokeTests` is a console app that exercises the full workflow against a **live** deployment: create airports → upload seat layout to S3 → create aircraft → schedule a flight. See [`tests/SmokeTests/README.md`](tests/SmokeTests/README.md).

```bash
dotnet run --project tests/SmokeTests -- https://embassyairlines.com/api/
```

It requires AWS credentials (it uploads a seat layout to S3 and resolves account/region via the AWS CLI). Because the backend is eventually consistent, the flight-scheduling step **retries on `404`** with exponential backoff — up to 5 attempts starting at 1 second — to absorb the propagation delay between creating an airport and the Flights context learning about it.

---

## Deliberate trade-offs

Choices made for a demonstration project that would differ in production:

- **Aircraft and Flights share one PostgreSQL instance**, logically separated by schema. Separate instances per service would be the production answer, but would multiply infrastructure cost for no additional architectural demonstration. The schema separation means splitting them later is straightforward.
- **Airports publishes inline rather than through an outbox.** Its write is a single DynamoDB item with no surrounding transaction, so there is no atomicity gap to close. The trade-off is real: a crash between the put and the publish loses the event.
- **A single staging environment**, not a full dev/staging/prod pipeline.
- **The Aspire AppHost wires only Airports.** Full local orchestration of all three services against emulated RDS/SNS/SQS was more setup than the functional tests already provide.
- **No NAT gateways.** Lambdas reach AWS services through VPC endpoints only. This is a cost and security win, but means no outbound internet access from inside the VPC.

---

## Extending the system

**Adding an HTTP endpoint.** Create a class implementing `IEndpoint` in the service's `Endpoints/` folder. Reflection-based discovery registers it — no manual wiring. Follow the existing pattern: validate with FluentValidation, return `ErrorOr`-based results mapped through `ErrorHandlingHelper`, and declare the response shape with `.Produces<T>()` / `.ProducesProblem()`.

**Adding a published event.** Three steps, all required — see [The transactional outbox](#the-transactional-outbox). Add the event record to `Shared.Contracts`, register the publisher in `OutboxProcessor.s_publishers`, register it in the Publisher Lambda's `AddAWSMessageBus`, and provision the SNS topic in `MessagingResources`.

**Adding a consumer.** Create a new `X.Api.Lambda.MessageHandlers.<EventName>` project with its own `Function` class and `HostApplicationBuilder`, add a matching dockerfile in `docker/`, and wire an `EventHandlerLambda` construct in CDK to create the queue, DLQ, and SNS subscription. Remember that the payload sits at `Message` → `data` in the SNS-over-SQS envelope.

**Changing a contract.** `Shared.Contracts` records are the wire format between independently deployed services. A breaking change needs a rollout plan — additive-optional fields first, or a versioned event — because producer and consumer will be running different builds during any deployment.
