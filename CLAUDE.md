# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Embassy Airlines is a demonstration event-driven microservices backend built on .NET 10 and AWS. It models three bounded contexts — **Airports**, **Aircraft**, and **Flights** — that communicate asynchronously via SNS topics and SQS queues and are eventually consistent. Every runtime component ships as a Docker-image AWS Lambda; infrastructure is provisioned with AWS CDK (also written in C#). Target region is `eu-west-2`.

## Build, test, and tooling commands

The solution file is `EmbassyAirlines.slnx` (the newer XML solution format — most `dotnet` commands pick it up automatically from the repo root). Tests run on **Microsoft.Testing.Platform** (selected in `global.json`), so pass a test project with `--project`.

```bash
# Build (warnings are errors — see below)
dotnet build

# Format: CI runs the verify form; run the plain form to auto-fix before committing
dotnet format --verify-no-changes
dotnet format

# Prettier gates non-C# files (JSON, YAML, Markdown) with default config
npx prettier --check .
npx prettier --write .

# Run all tests
dotnet test

# Run one test project
dotnet test --project tests/Aircraft.Api.Lambda.FunctionalTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~AircraftTests.CreateAircraft_ShouldReturnCreated"

# Coverage: runs the tests with coverlet, then writes an HTML report to Reports/CoverageReport/
# (requires ReportGenerator: dotnet tool install -g dotnet-reportgenerator-globaltool)
dotnet run scripts/Coverage.cs
```

**Docker is required to run the functional and integration tests** — they spin up PostgreSQL (and, for Aircraft, LocalStack) containers via Testcontainers.

CI (`.github/workflows/main.yml`, runs on push to `main`) has two parallel jobs: **Prettier** (`npx prettier --check .`) and **DotNet** (`dotnet format --verify-no-changes`, then `dotnet build -c Release`, then `dotnet test`). Both must pass.

### Code-quality enforcement (important)

`Directory.Build.props` applies to every project: `TreatWarningsAsErrors=true`, `AnalysisMode=All`, `EnforceCodeStyleInBuild=true`, plus SonarAnalyzer.CSharp. A build fails on any analyzer or style violation. `.editorconfig` promotes several conventions to **errors** (e.g. no `this.` qualification, predefined type keywords over BCL names). When a rule is genuinely not applicable, suppress it in `.editorconfig` (see the existing `dotnet_diagnostic.*.severity = none` entries) rather than inline. NuGet versions are centrally managed in `Directory.Packages.props` — reference packages without a `Version` attribute.

## Architecture

### Per-service layering

All three services share one layered structure:

- **`X.Core`** — domain models/aggregates with behavior (e.g. `Aircraft.Create(...)`, `aircraft.MarkAsParked(...)`). Entities extend the shared `Entity` base and raise domain events by calling `AddDomainEvent`.
- **`X.Infrastructure`** — EF Core `ApplicationDbContext`, entity configurations, migrations, `DesignTimeDbContextFactory`, the service's `OutboxProcessor`, and the idempotent write paths its message handlers call (Flights' `AircraftProjectionWriter` / `AirportProjectionWriter`, Aircraft's `AircraftFlightStatusProcessor`).
- **`X.Api.Lambda`** — an ASP.NET Core Minimal API hosted in Lambda via `AddHttpApiLambdaDefaults` (which calls `AddAWSLambdaHosting(LambdaEventSource.HttpApi)`). HTTP endpoints only.
- **`X.Publisher.Lambda`** — the outbox drainer (see below), invoked on a 1-minute schedule.
- **`X.Migrations.Lambda`** — applies the service's EF Core migrations as the handler of a CloudFormation custom resource during `cdk deploy`.
- **`X.Api.Lambda.MessageHandlers.<EventName>`** — one Lambda project **per consumed event**, each an SQS-triggered `Function` class that owns its own `HostApplicationBuilder` and processes batches with `SqsBatchProcessor` (partial batch failures). Airports consumes no events, so it has none.

All three services share one **PostgreSQL** database, logically separated by per-service schema (`airports`, `aircraft`, `flights`). Airports was originally on DynamoDB; nothing DynamoDB-related remains.

### Shared libraries

Cross-cutting code is split across projects by dependency footprint, so each Lambda references only what it needs; `src/Shared/README.md` tours all of them in depth. Each Lambda's dockerfile in `docker/` `COPY`s exactly the projects it builds against, so **adding a project reference to a Lambda also means adding a matching `COPY` line to its dockerfile**.

- **`src/Shared`** — dependency-free contracts and domain building blocks. Key pieces you will touch often:
    - **`Shared.Contracts`** — immutable record DTOs (`AircraftDto`, `ScheduleFlightDto`, …) and integration events (`AircraftCreatedEvent`, `FlightArrivedEvent`, …) implementing `IDomainEvent` / `IFlightStatusManagementEvent`. These records are the wire contract between services — changing one affects producer and consumer.
    - **`Entity`** — the aggregate base class that collects raised events (`AddDomainEvent`, `DomainEvents`, `ClearDomainEvents`).
    - **`Ensure`** — guard-clause helpers (`NotNullOrEmpty`, `ZeroOrGreater`, `GreaterThanZero`, …) using `CallerArgumentExpression`. Prefer these over hand-written argument checks.
- **`src/Shared.TransactionalOutbox`** — database- and publisher-agnostic outbox primitives: `OutboxMessage`, `OutboxConstants`, `OutboxJson`, `IOutboxProcessor`, and `OutboxProcessorBase<TPublisher>` (publish/retry/dead-letter policy).
- **`src/Shared.Npgsql`** — builds on `Shared` and `Shared.TransactionalOutbox`: `AddDatabaseConnection<TDbContext>` (an IAM/RDS Proxy overload and a plain connection-string overload used by tests), `AddOutboxInterceptor` (registers `InsertOutboxMessagesInterceptor`, which `AddDatabaseConnection` attaches to the `DbContext` only if it is registered), `NpgsqlOutboxProcessorBase<TPublisher>` (claim → publish → record-outcome loop), `DatabaseClock`, and `ApplyMigrationsAsync`.
- **`src/Shared.ServiceDefaults`** — `AddServiceDefaults()`, generic over `IHostApplicationBuilder`: OpenTelemetry logging, metrics, and tracing (ASP.NET Core, `HttpClient`, runtime, AWS SDK, AWS.Messaging, and Lambda instrumentation), service discovery, and standard `HttpClient` resilience. Every API, publisher, and message-handler Lambda calls it; the migration Lambdas do not.
- **`src/Shared.HttpApiLambdaDefaults`** — the ASP.NET Core layer, referenced only by the three API Lambdas (and, through them, the functional tests). It references `Shared.ServiceDefaults`.
    - **`AddHttpApiLambdaDefaults(assembly)`** — `AddServiceDefaults` plus endpoint discovery, `AddAWSLambdaHosting(LambdaEventSource.HttpApi)`, `GlobalExceptionHandler`, `ProblemDetails`, and OpenAPI. Pair it with `UseDefaultPipeline()` (exception handler, correlation-id middleware, Serilog request logging) and `MapDefaultEndpoints()` (the discovered endpoints, plus the OpenAPI document in `Development`).
    - **`IEndpoint`** — each Minimal API endpoint is a class implementing `MapEndpoint`; `AddEndpoints(assembly)` + `MapEndpoints()` discover and register them via reflection, so new endpoints need no manual registration.
    - **Error handling** — endpoints return `ErrorOr`-based results mapped through `ErrorHandlingHelper.GetProblemDetails` and `GlobalExceptionHandler` into RFC-compliant `ProblemDetails`. Follow the existing endpoint pattern rather than throwing. `IExceptionHandler`s run in registration order, so a service-specific handler registered before `AddHttpApiLambdaDefaults` runs ahead of `GlobalExceptionHandler` (Flights does this with `ConcurrencyExceptionHandler`).
    - **`RequestContextLoggingMiddleware`** — enriches Serilog logs with a correlation id (`X-Correlation-Id` header, else trace id).
    - **Test helpers** — `ReadProblemDetailsAsync` and `CreateExpectedProblemDetails`, which each `BaseFunctionalTest` uses for its `ProblemDetails` assertions.
- **`src/Shared.Messaging`** — `SqsBatchProcessor`, the partial-batch-failure loop shared by the six message-handler Lambdas. Depends only on `Amazon.Lambda.Core` and `Amazon.Lambda.SQSEvents`.
- **`src/Shared.AWS.CloudWatchLogs`** — helper used by the log-fetching scripts.

### Transactional outbox (all services)

Events are **not** published inline. Instead:

1. Domain models raise events into `Entity.DomainEvents` during a transaction.
2. `InsertOutboxMessagesInterceptor` (an EF `SaveChangesInterceptor`) serializes those events into `outbox_messages` in the **same transaction** as the state change, then clears them after save. It is opt-in: the API Lambdas, the only code paths that raise events, register it with `AddDatabaseConnection(...).AddOutboxInterceptor()`. Without that call, raised events are silently not persisted.
3. The **Publisher Lambda** runs every minute. It claims due rows with a single statement (a `SELECT ... FOR UPDATE SKIP LOCKED` CTE feeding an `UPDATE ... RETURNING` that stamps `claim_id` + `claimed_until_utc`), then publishes to SNS via the `AWS.Messaging` bus, recording each message's outcome individually with exponential-backoff retries and dead-lettering. The loop is shared (`NpgsqlOutboxProcessorBase` + `OutboxProcessorBase`); the base also owns the claim SQL, so each service's `OutboxProcessor` supplies only its schema name and `s_publishers` registry. Lease timing belongs to PostgreSQL: the expiry is stamped as `now() + OutboxConstants.ClaimDuration` and every check of it compares against `now()` (written in LINQ as `DatabaseClock.UtcNow()`), so clock skew between invocations cannot lengthen, shorten, or misjudge a lease. Claims expire after `ClaimDuration` so messages are not stranded when an invocation dies mid-batch. Each outcome is written by a single `UPDATE` conditioned on both the claim id and an unexpired `claimed_until_utc`, so an invocation that overran its lease cannot write state it no longer owns; if that `UPDATE` matches no rows, or fails outright, the worker abandons the rest of the claim rather than publishing messages whose outcomes it may equally be unable to record. The loop also stops publishing once the time elapsed since it claimed is within `OutboxConstants.ClaimSafetyMargin` of `ClaimDuration` — a local optimisation only, since ownership is decided in the database.

When adding a new published event: raise it from the domain model, register a publisher for it in that service's `OutboxProcessor.s_publishers` dictionary **and** its Publisher Lambda's `AddAWSMessageBus` configuration (Flights keeps this in `AwsMessageBusInstaller.cs`), then provision the SNS topic in `MessagingResources`, pass its ARN as `SNS:<EventName>TopicArn`, and add it to the `PublisherLambda` construct's `Topics`. Missing registration dead-letters the message.

### Consumers, idempotency, and concurrency

Delivery is at-least-once and unordered, so every consumer must be idempotent. Handlers parse the SNS-over-SQS envelope (`Message` → `data`) and **throw** on malformed payloads rather than acknowledging them; failed messages are retried individually and dead-lettered after 3 receives.

- **Flights** maintains its aircraft/airport read models with atomic upserts: aircraft are inserted with `ON CONFLICT DO NOTHING`; airport snapshots apply only when their aggregate `Version` is newer than the stored one.
- **Aircraft** uses a transactional inbox: `AircraftFlightStatusProcessor` inserts the event id into `inbox_messages` in the same transaction as the aircraft update and returns `Applied`, `Stale` (event `OccurredAt` ≤ aircraft `UpdatedAt`; inbox row still committed), or `Duplicate`. The flight status events (`FlightArrived`, `FlightMarkedAsEnRoute`, `FlightMarkedAsDelayedEnRoute`) carry `OccurredAt` for this.
- **Optimistic concurrency**: Airports uses an application-managed `Version` concurrency token; `Flight` and `Aircraft` use PostgreSQL's `xmin` as an EF row version. in Flights, a `DbUpdateConcurrencyException` from a `PATCH` endpoint propagates to `ConcurrencyExceptionHandler`, which returns `409 Conflict`; `AircraftFlightStatusProcessor` rolls back, reloads, and retries (up to 3 attempts).

### Database access

`AddDatabaseConnection` builds an Npgsql data source that authenticates to **RDS Proxy** using IAM tokens (`RDSAuthTokenGenerator`) — there is no static password; `SslMode.Require` is enforced. Each service uses a dedicated schema (e.g. `aircraft`) with snake_case naming (`UseSnakeCaseNamingConvention`).

Migrations are applied in two ways. In deployed environments, each service's `X.Migrations.Lambda` runs them via a CloudFormation custom resource (`DatabaseMigrationLambda` in CDK) that re-runs whenever the migration image changes, and the service's other Lambdas depend on it. In the `Development` environment (which `WebApplicationFactory` uses), the API applies them at startup via `ApplyMigrationsAsync`.

To add a migration, use the `DesignTimeDbContextFactory` in each `X.Infrastructure` project (it supplies a throwaway local connection string, so no live DB is needed). `Microsoft.EntityFrameworkCore.Design` is referenced by the `X.Api.Lambda` projects rather than the `X.Infrastructure` projects, so pass one as the startup project:

```bash
dotnet ef migrations add <Name> --project src/Aircraft.Infrastructure --startup-project src/Aircraft.Api.Lambda
```

### Configuration

Each service reads environment variables under a **service-specific prefix**: `AIRCRAFT_`, `FLIGHTS_`, `AIRPORTS_` (e.g. `AIRCRAFT_DbConnection__Host`). Keys: `DbConnection:{Host,Database,Username,Port}`, `SNS:<EventName>TopicArn` (publishers), and `S3:BucketName` (Aircraft API). Outbox tuning (batch size, retry attempts and delays, claim duration, safety margin) is **not** runtime-configurable — it lives in `OutboxConstants` in `src/Shared.TransactionalOutbox`.

## Local development

There is **no local orchestration**. The former .NET Aspire AppHost (which ran Airports against DynamoDB Local) was removed when Airports moved to PostgreSQL. The test suites are the local harness; running an API with `dotnet run` is not supported as-is, because under `Development` it expects the host to register the `DbContext`, which only the test factories do.

## Deployment (`Deployment/`)

A C# AWS CDK app (`cdk.json` → `dotnet run --project Deployment/Deployment.csproj`). `EmbassyAirlinesStack` composes networking (isolated-subnet VPC, no NAT, VPC endpoints for S3/SNS/SQS), SNS topics (`MessagingResources`), a shared API Gateway custom domain (`SharedInfra`), shared RDS + RDS Proxy (`RdsResources`), and the three service constructs. Reusable Lambda constructs live in `Deployment/Lambdas/`: `HttpDockerLambda` (API + route + DB access), `EventHandlerLambda` (SQS + DLQ with 14-day retention + SNS subscription), `PublisherLambda` (scheduled outbox drainer), `DatabaseMigrationLambda` (migrations custom resource). Every Lambda is a Docker image built from a dockerfile in `docker/`. See `Deployment/README.md` for the full infrastructure breakdown.

```bash
cdk synth
cdk deploy
```

## Tests

All automated suites use xUnit v3 on Microsoft.Testing.Platform, asserting with **AwesomeAssertions** (the Apache-licensed FluentAssertions fork).

- **Functional tests** (`tests/*.Api.Lambda.FunctionalTests`) use `WebApplicationFactory<Program>`, which runs the app under `Development`, so it skips the RDS/IAM `AddDatabaseConnection` path and applies migrations at startup. `FunctionalTestWebAppFactory` registers the `DbContext` against Testcontainers PostgreSQL (`postgres:18`), plus LocalStack S3 for Aircraft. `BaseFunctionalTest` provides the shared `HttpClient` and `ProblemDetails` assertion helpers. Flights' `ConcurrencyFunctionalTestWebAppFactory` injects an interceptor that forces a concurrency conflict.
- **Integration tests** (`tests/*.Infrastructure.IntegrationTests`) exercise the outbox, the Aircraft inbox, read-model projection writers, and optimistic concurrency against a real PostgreSQL container.
- **Unit tests** (`tests/Shared.Npgsql.UnitTests`) cover `AddDatabaseConnection` registration.
- **Smoke tests** (`tests/SmokeTests`) is a console app that exercises the full create-airports → upload-layout → create-aircraft → schedule-flight workflow against a **live** deployment, with retry/backoff for eventual consistency. Requires AWS credentials (uploads a seat layout to S3, resolves account/region via the AWS CLI):

    ```bash
    dotnet run --project tests/SmokeTests -- https://embassyairlines.com/api/
    ```

## Utility scripts

`scripts/*.cs` are file-based C# apps (`#:project` directive). `Coverage.cs` produces the coverage report (see above); `Get{Aircraft,Airports,Flights}Logs.cs` fetch recent CloudWatch logs for that service's API Lambda and require AWS credentials:

```bash
dotnet run scripts/GetAircraftLogs.cs
```
