# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Embassy Airlines is a demonstration event-driven microservices backend built on .NET 10 and AWS. It models three bounded contexts — **Airports**, **Aircraft**, and **Flights** — that communicate asynchronously via SNS topics and SQS queues and are eventually consistent. Every runtime component ships as a Docker-image AWS Lambda; infrastructure is provisioned with AWS CDK (also written in C#). Target region is `eu-west-2`.

## Build, test, and tooling commands

The solution file is `EmbassyAirlines.slnx` (the newer XML solution format — most `dotnet` commands pick it up automatically from the repo root).

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
dotnet test tests/Aircraft.Api.Lambda.FunctionalTests

# Run a single test by name
dotnet test --filter "FullyQualifiedName~AircraftTests.CreateAircraft_ShouldReturnCreated"

# Test with coverage
dotnet test --settings coverlet.runsettings
```

**Docker is required to run the functional tests** — they spin up PostgreSQL and LocalStack containers via Testcontainers.

CI (`.github/workflows/main.yml`, runs on push to `main`) enforces, in order: `prettier --check`, `dotnet format --verify-no-changes`, `dotnet build -c Release`, `dotnet test`. All four must pass.

### Code-quality enforcement (important)

`Directory.Build.props` applies to every project: `TreatWarningsAsErrors=true`, `AnalysisMode=All`, `EnforceCodeStyleInBuild=true`, plus SonarAnalyzer.CSharp. A build fails on any analyzer or style violation. `.editorconfig` promotes several conventions to **errors** (e.g. no `this.` qualification, predefined type keywords over BCL names). When a rule is genuinely not applicable, suppress it in `.editorconfig` (see the existing `dotnet_diagnostic.*.severity = none` entries) rather than inline. NuGet versions are centrally managed in `Directory.Packages.props` — reference packages without a `Version` attribute.

## Architecture

### Per-service layering

Each service (Aircraft, Flights) is split into layered projects; Airports is a slimmer variant with no database layer:

- **`X.Core`** — domain models/aggregates with behavior (e.g. `Aircraft.Create(...)`, `aircraft.MarkAsParked(...)`). Entities extend the shared `Entity` base and raise domain events by calling `AddDomainEvent`.
- **`X.Infrastructure`** — EF Core `ApplicationDbContext`, entity configurations, migrations, the outbox implementation, and `AddDatabaseConnection` DI wiring.
- **`X.Api.Lambda`** — an ASP.NET Core Minimal API hosted in Lambda (`AddAWSLambdaHosting(LambdaEventSource.HttpApi)`). HTTP endpoints only.
- **`X.Publisher.Lambda`** — the outbox drainer (see below), invoked on a 1-minute schedule.
- **`X.Api.Lambda.MessageHandlers.<EventName>`** — one Lambda project **per consumed event**, each an SQS-triggered `Function` class that owns its own `HostApplicationBuilder`.

Airports uses **DynamoDB** (no `Core`/`Infrastructure` split); Aircraft and Flights share a **PostgreSQL** database (logically separated by per-service schema).

### Shared library (`src/Shared`)

Cross-cutting contracts and infrastructure used by every service — read `src/Shared/README.md` for the full tour. Key pieces you will touch often:

- **`Shared.Contracts`** — immutable record DTOs (`AircraftDto`, `ScheduleFlightDto`, …) and integration events (`AircraftCreatedEvent`, `FlightArrivedEvent`, …) implementing `IDomainEvent` / `IFlightStatusManagementEvent`. These records are the wire contract between services — changing one affects producer and consumer.
- **`IEndpoint`** — each Minimal API endpoint is a class implementing `MapEndpoint`; `AddEndpoints(assembly)` + `MapEndpoints()` discover and register them via reflection, so new endpoints need no manual registration.
- **`Ensure`** — guard-clause helpers (`NotNullOrEmpty`, `GreaterThanZero`, …) using `CallerArgumentExpression`. Prefer these over hand-written argument checks.
- **Error handling** — endpoints return `ErrorOr`-based results mapped through `ErrorHandlingHelper.GetProblemDetails` and `GlobalExceptionHandler` into RFC-compliant `ProblemDetails`. Follow the existing endpoint pattern rather than throwing.
- **`RequestContextLoggingMiddleware`** — enriches Serilog logs with a correlation id (`X-Correlation-Id` header, else trace id).

### Transactional outbox (Aircraft & Flights)

Events are **not** published inline. Instead:

1. Domain models raise events into `Entity.DomainEvents` during a transaction.
2. `InsertOutboxMessagesInterceptor` (an EF `SaveChangesInterceptor` registered in `AddDatabaseConnection`) serializes those events into `outbox_messages` in the **same transaction** as the state change, then clears them after save.
3. The **Publisher Lambda** runs every minute, selects due rows with `FOR UPDATE SKIP LOCKED`, and publishes to SNS via the `AWS.Messaging` bus, with exponential-backoff retries and dead-lettering (`OutboxProcessor`).

When adding a new published event: raise it from the domain model, and register a publisher for it in that service's `OutboxProcessor.s_publishers` dictionary **and** its Publisher Lambda's `AddAWSMessageBus` configuration. Missing registration dead-letters the message.

### Database access

`AddDatabaseConnection` builds an Npgsql data source that authenticates to **RDS Proxy** using IAM tokens (`RDSAuthTokenGenerator`) — there is no static password; `SslMode.Require` is enforced. Each service uses a dedicated schema (e.g. `aircraft`) with snake_case naming (`UseSnakeCaseNamingConvention`). Migrations are applied automatically at API startup via `ApplyMigrationsAsync` (`Database.MigrateAsync()`).

To add a migration, use the `DesignTimeDbContextFactory` in each `X.Infrastructure` project (it supplies a throwaway local connection string, so no live DB or startup project is needed):

```bash
dotnet ef migrations add <Name> --project src/Aircraft.Infrastructure
```

### Configuration

Each service reads environment variables under a **service-specific prefix**: `AIRCRAFT_`, `FLIGHTS_`, `AIRPORTS_` (e.g. `AIRCRAFT_DbConnection__Host`). Common keys: `DbConnection:{Host,Database,Username,Port}`, `SNS:<EventName>TopicArn`, `S3:BucketName`, and `Outbox:{BatchSize,MaxRetryAttempts,BaseRetryDelaySeconds,MaxRetryDelaySeconds}`.

## Local development

`src/AWS.Aspire.AppHost` is a .NET Aspire host that runs services locally against emulators (DynamoDB Local, the AWS Lambda service emulator, and an API Gateway emulator on port 3000). Note it currently wires up only the Airports service.

```bash
dotnet run --project src/AWS.Aspire.AppHost
```

## Deployment (`Deployment/`)

A C# AWS CDK app (`cdk.json` → `dotnet run --project Deployment/Deployment.csproj`). `EmbassyAirlinesStack` composes networking (isolated-subnet VPC), SNS topics (`MessagingResources`), a shared API Gateway custom domain (`SharedInfra`), shared RDS + RDS Proxy, and the three service constructs. Reusable Lambda constructs live in `Deployment/Lambdas/`: `HttpDockerLambda` (API + route + DB access), `EventHandlerLambda` (SQS + DLQ + SNS subscription), `PublisherLambda` (scheduled outbox drainer). Every Lambda is a Docker image built from a dockerfile in `docker/`. See `Deployment/README.md` for the full infrastructure breakdown.

```bash
cdk synth
cdk deploy
```

## Tests

- **Functional tests** (`tests/*.FunctionalTests`) use `WebApplicationFactory<Program>` + xUnit v3 + FluentAssertions (pinned to 7.0.0). `FunctionalTestWebAppFactory` boots the real app under the `FunctionalTests` environment (which skips the RDS/IAM `AddDatabaseConnection` and substitutes Testcontainers PostgreSQL + LocalStack). `BaseFunctionalTest` provides the shared `HttpClient` and `ProblemDetails` assertion helpers.
- **Smoke tests** (`tests/SmokeTests`) is a console app that exercises the full create-airports → upload-layout → create-aircraft → schedule-flight workflow against a **live** deployment, with retry/backoff for eventual consistency. Requires AWS credentials (uploads a seat layout to S3, resolves account/region via the AWS CLI):

    ```bash
    dotnet run --project tests/SmokeTests -- https://embassyairlines.com/api/
    ```

## Utility scripts

`scripts/*.cs` are file-based C# apps (`#:project` directive) that fetch recent CloudWatch logs for a given Lambda. Run directly and require AWS credentials:

```bash
dotnet run scripts/GetAircraftLogs.cs
```
