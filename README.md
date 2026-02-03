# EmbassyAirlines

Embassy Airlines is a cloud-native airline management platform built with .NET 10, Docker and AWS CDK. It currently provides APIs for managing aircraft, airports, and flights. These APIs leverage AWS services such as Lambda, DynamoDB, RDS, S3, and SNS. Additional APIs are planned but not yet implemented.

## Project Structure \& Infrastructure

- **Multi-service architecture** with 3 main APIs and shared libraries
- **AWS CDK deployment** configuration for cloud infrastructure
- **Docker containerization** for each service
- **GitHub Actions CI/CD pipeline** with build and test stages

## Core Services

### 1\. **Aircraft API (Lambda)**

- Manages aircraft fleet with seat configurations
- **PostgreSQL database** with Entity Framework Core
- **S3 integration** for manually-uploaded seat layout definitions (JSON files)
- **SNS publishing** for aircraft creation events
- **Event-driven architecture** consuming flight status management events via SQS
- Complex seat layout system with business/economy configurations facilitated by manually-uploaded JSON files in S3. Example JSON files reside in the Resources/Layouts folder.

### 2\. **Airports API (Lambda)**

- Airport management with IATA/ICAO codes and timezone handling
- **DynamoDB storage** with custom repository pattern
- **SNS publishing** for airport created/updated events
- Full CRUD operations with validation

### 3\. **Flights API (Lambda)**

- Flight scheduling and management system
- **PostgreSQL database** with NodaTime for timezone-aware scheduling
- **Event-driven architecture** consuming aircraft/airport events via SQS
- **SNS publishing** for flight scheduling and flight operations management

#### Advanced Timezone Handling

The Flights service uses **NodaTime** for robust timezone-aware scheduling that handles real-world complexities:

**Key components:**

- **SchedulingAmbiguityPolicy enum** — Controls behavior during DST transitions:
    - `ThrowWhenAmbiguous` — Rejects scheduling during ambiguous times (e.g., when clocks fall back)
    - `PreferEarlier` — Chooses the earlier occurrence during ambiguous times
    - `PreferLater` — Chooses the later occurrence during ambiguous times

- **InZone() extension method** — Converts UTC `Instant` values to local `ZonedDateTime` in a specific timezone
    - Used extensively in flight scheduling tests
    - Ensures departure/arrival times are correctly interpreted in local airport timezones

- **ZoneLocalMappingResolver** — NodaTime's resolver system for handling DST edge cases:
    - Configured via `FromSchedulingAmbiguityPolicy()` extension method in Flights.Core
    - Always uses `Resolvers.ThrowWhenSkipped` for skipped times (e.g., spring forward)
    - User-selected policy determines behavior for ambiguous times (fall back)

**Example DST handling:**

When scheduling a flight departing at 2:30 AM on a DST transition day:

- If clocks fall back (2:00 AM → 1:00 AM), the local time 2:30 AM occurs twice
- `ThrowWhenAmbiguous` rejects the request, forcing explicit clarification
- `PreferEarlier` chooses the first 2:30 AM (before the fallback)
- `PreferLater` chooses the second 2:30 AM (after the fallback)

This design ensures **flight times are never ambiguous** and clients explicitly handle edge cases rather than silently accepting potentially incorrect schedules.

## Technical Features

- **Shared library** with common contracts, validation, error handling, and middleware
- **Comprehensive validation** using FluentValidation
- **Error handling** with ErrorOr pattern and standardized problem details
- **Structured logging** with Serilog and correlation IDs
- **OpenTelemetry distributed tracing** with Activity-based instrumentation in message handlers, correlation ID propagation, and detailed entity tagging for observability
- **Extensive functional tests** with test containers (PostgreSQL, DynamoDB, LocalStack)
- **Code quality enforcement** with EditorConfig, analyzers, and formatting rules

## Architecture Patterns

### Domain-Driven Design (DDD) Patterns

- **Bounded Contexts** in the Aircraft.Core and Flights.Core class library projects
- Aircraft.Core.Models.Aircraft and Flights.Core.Models.Flight are **aggregate roots**
- **Entities** show up clearly via identity (Id) attributes, lifecycle timestamps and behavior-driven state mutations.
- Immutable, validated, concept-focused **value objects** such as Weight and Money
- Consistent hiding of constructors and exposure of static **factory methods**
- Clean **repository** interface and implementation in Airports.Api.Lambda

**Architectural variation in Airports service:**

The Airports.Api.Lambda differs from the Aircraft and Flights services by **placing domain logic directly in the Lambda project** without a separate `.Core` bounded context library. This is an intentional design choice because:

- The Airport entity has simpler business logic compared to Aircraft and Flight
- There are no complex value objects or aggregate relationships
- CRUD operations dominate over complex domain behavior

This demonstrates that **bounded context complexity should match domain complexity** — not every service requires the same level of architectural separation.

**Dual Airport representations:**

The system contains two distinct `Airport` entity representations:

- **Airports.Api.Lambda.Airport** — The authoritative source of truth with full domain logic and persistence
- **Flights.Core.Models.Airport** — A lightweight read model synchronized via events (see "Read models and CQRS pattern" section)

This duality is intentional and reflects the **CQRS pattern** where each service maintains its own optimized representation of cross-cutting entities.

### Event-Driven Architecture (EDA)

This system uses **asynchronous, event-driven communication** to decouple services and improve resilience.

#### High-level pattern

- Services **publish domain events** to SNS topics
- Each event type is delivered to one or more **SQS queues**
- Lambda functions consume messages from queues and execute handlers
- Each queue has an associated **Dead Letter Queue (DLQ)**

This design provides:

- **Loose coupling** between services
- **Independent scaling** of producers and consumers
- **Failure isolation** (one failing consumer does not block others)
- **Retry and recovery** via SQS redrive policies

Services never call each other directly for domain state changes.

#### Read models and CQRS pattern

The Flights service maintains **lightweight read models** of entities from other bounded contexts to support query operations and validation without cross-service API calls.

**Current read models in Flights.Core:**

- **Airport** — Cached representation of airport data (IataCode, IcaoCode, Name, TimeZoneId)
- **Aircraft** — Cached representation of aircraft data (TailNumber, EquipmentCode)

These read models are:

- Created and updated via **event handlers** consuming `AircraftCreatedEvent`, `AirportCreatedEvent`, and `AirportUpdatedEvent`
- Stored in the Flights PostgreSQL database alongside the Flight aggregate root
- Used for **query-side operations** such as validating flight scheduling requests
- Deliberately simplified — they contain only the subset of data needed by the Flights service

This demonstrates a **CQRS-like pattern** where:

- The **command side** (source of truth) lives in the owning service (Aircraft.Api.Lambda, Airports.Api.Lambda)
- The **query side** (denormalized read model) lives in the consuming service (Flights.Api.Lambda)
- Synchronization happens asynchronously via domain events

**Benefits:**

- Flights can validate that referenced airports and aircraft exist without synchronous HTTP calls
- Each service maintains its own optimized query model
- Services remain loosely coupled and can scale independently
- Read models can be rebuilt by replaying events if needed

**Trade-offs:**

- Eventual consistency — there may be a brief delay between an aircraft being created and being available for flight scheduling
- Storage overhead — data is duplicated across services
- Maintenance complexity — event handlers must be kept in sync with schema changes

#### Event publishing

When a meaningful domain action occurs (e.g. an entity is created or updated):

- The owning service publishes a domain event to its SNS topic
- Events should be:
    - Immutable
    - Explicitly versioned if schemas evolve
    - Focused on _what happened_, not _what should happen_

At the moment no events are published to signalize entity deletion and there are no HTTP DELETE endpoints. This is because I want to preserve an accurate historical record of airline operations which have completed.

#### Event consumption

Consumers subscribe via SQS queues:

- Each queue represents a **single responsibility** handler
- Lambda functions process messages in batches
- Handlers are expected to be **idempotent**
- Failures result in retries; repeated failures move messages to the DLQ

#### Dead Letter Queues (DLQs)

Every consumer queue has a DLQ:

- Messages land in the DLQ after exceeding retry limits
- DLQs are a **signal**, not an error sink

Operational expectations:

- DLQs should be monitored
- Messages should be inspected and replayed when appropriate
- Silent DLQ growth indicates a broken consumer or contract mismatch

#### Event Flow Status

The system currently defines 12 SNS topics, of which 6 have active publishers and consumers. The remaining 6 topics are provisioned but have no current consumers, representing planned event flows.

**Implemented Event Flows:**

| Integration Event                 | Publisher           | Consumer                                                         |
| --------------------------------- | ------------------- | ---------------------------------------------------------------- |
| AircraftCreatedEvent              | Aircraft.Api.Lambda | Flights.Api.Lambda.MessageHandlers.AircraftCreated               |
| AirportCreatedEvent               | Airports.Api.Lambda | Flights.Api.Lambda.MessageHandlers.AirportCreated                |
| AirportUpdatedEvent               | Airports.Api.Lambda | Flights.Api.Lambda.MessageHandlers.AirportUpdated                |
| FlightArrivedEvent                | Flights.Api.Lambda  | Aircraft.Api.Lambda.MessageHandlers.FlightArrived                |
| FlightMarkedAsDelayedEnRouteEvent | Flights.Api.Lambda  | Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute |
| FlightMarkedAsEnRouteEvent        | Flights.Api.Lambda  | Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsEnRoute        |

**Planned Event Flows (Topics provisioned, no current consumers):**

| Integration Event           | SNS Topic                       | Status       |
| --------------------------- | ------------------------------- | ------------ |
| FlightScheduledEvent        | FlightScheduledTopic            | No consumers |
| FlightAircraftAssignedEvent | AircraftAssignedToFlightTopic   | No consumers |
| FlightPricingAdjustedEvent  | FlightPricingAdjustedTopic      | No consumers |
| FlightRescheduledEvent      | FlightRescheduledTopic          | No consumers |
| FlightCancelledEvent        | FlightCancelledTopic            | No consumers |
| FlightDelayedEvent          | FlightDelayedTopic              | No consumers |
| AircraftUpdatedEvent        | AircraftUpdatedTopic (if added) | No consumers |

#### Adding a new event or consumer

1. Define the domain event as a record type
2. Use a primary constructor unless there are a lot of properties
3. Publish the event from the owning service
4. Create or reuse an SNS topic
5. Add an SQS queue subscription
6. Implement a Lambda handler
7. Configure retries and DLQ
8. Add tests for publishing and handling

#### Design philosophy

Events are treated as **first-class domain concepts**, not side effects.
If a workflow feels difficult to express with events, that is usually a signal to revisit:

- Service boundaries
- Event granularity
- Ownership of state

### Middleware Pipelines

This project uses a simple but effective ASP.NET Core middleware pipeline to ensure **consistent error handling**, **structured logging**, and **request traceability** across the application. The pipeline is composed of a custom exception handler and a request context logging middleware.

#### Global Exception Handling

The `GlobalExceptionHandler` implements `IExceptionHandler` and acts as a centralized mechanism for catching and handling unhandled exceptions.

**Responsibilities:**

- Catches any unhandled exception thrown during request processing.
- Logs the exception with structured logging using `ILogger`.
- Returns a standardized RFC 7231–compliant `ProblemDetails` JSON response.
- Ensures clients always receive a consistent `500 Internal Server Error` response format.

**Key benefits:**

- Prevents exception details from leaking to clients.
- Improves observability by logging full exception context.
- Eliminates repetitive try/catch logic in controllers and endpoints.

This handler is designed to be registered with ASP.NET Core’s built-in exception handling infrastructure, keeping error handling centralized and declarative.

#### Request Context Logging Middleware

The `RequestContextLoggingMiddleware` enriches all log entries for a request with a **Correlation ID**, enabling end-to-end request tracing.

**How it works:**

- Reads the `X-Correlation-Id` header from incoming requests.
- Falls back to `HttpContext.TraceIdentifier` if the header is missing.
- Pushes the correlation ID into the logging scope using `LogContext`.
- Ensures all logs written during the request include the same correlation identifier.

**Key benefits:**

- Makes it easy to trace a single request across logs.
- Improves debugging in distributed or microservice-based systems.
- Requires no changes to controllers or business logic.

## Infrastructure as Code (IaC)

Within the `Deployment` project there is an `EmbassyAirlinesStack` which inherits from the Amazon.CDK.Stack base class.

`EmbassyAirlinesStack` provisions a small, event-driven microservices backend on AWS. It combines:

- **A shared HTTP entrypoint** (API Gateway HTTP API) exposed under a custom domain (`embassyairlines.com/api/*`)
- **Three domain services** implemented as **container-image Lambda functions**:
    - Airports (DynamoDB-backed)
    - Flights (PostgreSQL-backed via RDS Proxy)
    - Aircraft (PostgreSQL-backed via RDS Proxy + S3 bucket)

- **SNS topics** as the service-to-service event bus
- **A private network** (VPC) with **isolated subnets only** and **VPC endpoints** so workloads can run without NAT

This configuration should be used only in non-production environments unless explicitly modified because many resources are configured with **RemovalPolicy.DESTROY** to facilitate experimentation and reduce costs.

### Stack composition (what gets created)

#### 1) Networking (VPC)

`Network` creates a VPC (`MaxAzs=2`) with **no NAT gateways** and relies on private connectivity:

- **Private isolated subnets** are used by Lambdas and RDS.
- VPC endpoints:
    - **S3 Gateway Endpoint** (for Aircraft service S3 access)
    - **DynamoDB Gateway Endpoint** (for Airports DynamoDB access)
    - **SNS Interface Endpoint** (for publishing/consuming SNS without public egress)
    - **SQS Interface Endpoint** (for sending/receiving SQS messages without public egress)

**Implication:** anything that requires public internet egress (e.g., pulling external APIs, calling 3rd-party services) would not work unless additional egress is introduced.

#### 2) Messaging (SNS topics)

`MessagingResources` defines the system’s event channels as **SNS Topics**, including:

- Airport events: `AirportCreatedTopic`, `AirportUpdatedTopic`
- Aircraft events: `AircraftCreatedTopic`
- Flight lifecycle events:
  `FlightScheduledTopic`, `AircraftAssignedToFlightTopic`, `FlightPricingAdjustedTopic`, `FlightRescheduledTopic`, `FlightCancelledTopic`, `FlightDelayedTopic`, `FlightMarkedAsEnRouteTopic`, `FlightMarkedAsDelayedEnRouteTopic`, `FlightArrivedTopic`

These topics are used both for **publishing domain events from APIs** and for **triggering handler Lambdas** through an SQS fan-out pattern (below).

---

#### 3) Shared Infra (API + domain)

`SharedInfra` centralizes the public ingress:

- **Route 53 Hosted Zone**: `embassyairlines.com` (imported by ID)
- **ACM Certificate** validated via DNS; explicitly **retained** on deletion
- **API Gateway v2 HTTP API** with a custom domain mapping:
    - Custom domain: `embassyairlines.com`
    - Mapping key: `api`
    - **Route53 A-record alias** to the API Gateway domain

This gives all services a single consistent entrypoint and domain.

---

#### 4) Data layer (PostgreSQL + RDS Proxy)

`RdsResources` provisions a relational backend used by Flights and Aircraft:

- **RDS PostgreSQL instance**:
    - Engine: Postgres `18.1`
    - Instance type: `t4g.micro`
    - Storage: 20GB
    - Subnets: private isolated
    - Credentials: generated secret for user `embassyadmin`
    - RemovalPolicy: **DESTROY**, no deletion protection (dev/test-friendly)

- **RDS Proxy**:
    - IAM auth enabled
    - Uses the DB secret
    - Dedicated security group

- Security: DB allows default port access from the proxy SG

**Pattern:** application Lambdas connect to **RDS Proxy**, not directly to the DB.

---

### Services (APIs + event handlers)

#### A) AirportsService (HTTP + DynamoDB + events)

**Purpose:** Airport CRUD-ish service.

Resources and flow:

- **DynamoDB table** `airports` (PAY_PER_REQUEST, partition key `Id` string)
- **Lambda (container image)** `AirportsApiLambda`
    - Runs in VPC private isolated subnet
    - Env config includes:
        - `AIRPORTS_DynamoDb__TableName`
        - `AIRPORTS_SNS__AirportCreatedTopicArn`
        - `AIRPORTS_SNS__AirportUpdatedTopicArn`

- **API route:** `ANY /airports` → Lambda integration
- Permissions:
    - Lambda has read/write to DynamoDB
    - Lambda can publish to AirportCreated and AirportUpdated topics

**Synchronous:** HTTP requests to `/airports`
**Asynchronous output:** publishes airport created/updated events to SNS

---

#### B) FlightsService (HTTP + Postgres via Proxy + publishes and consumes events)

**Purpose:** Flight operations + coordination.

Resources and flow:

- **Lambda (container image)** `FlightsApiLambda`
    - VPC private isolated
    - Common DB env:
        - `FLIGHTS_DbConnection__Database`, `Host` (proxy endpoint), `Port`, `Username`

    - SNS env includes many topic ARNs for flight lifecycle events

- **API route:** `ANY /flights`
- Permissions:
    - Can connect to RDS Proxy (IAM auth)
    - SG allows egress to DB proxy SG on 5432
    - Can publish to at least `FlightScheduledTopic` (explicitly granted in the snippet)

**Event consumption:** Flights also reacts to other services via handler Lambdas (below):

- `FlightsAircraftCreatedHandlerLambda` subscribes to `AircraftCreatedTopic`
- `FlightsAirportCreatedHandlerLambda` subscribes to `AirportCreatedTopic`
- `FlightsAirportUpdatedHandlerLambda` subscribes to `AirportUpdatedTopic`

These handlers update/query the Flights DB via the proxy.

---

#### C) AircraftService (HTTP + Postgres via Proxy + S3 + consumes flight events)

**Purpose:** Aircraft management, with artifact storage and reactions to flight state.

Resources and flow:

- **S3 bucket** `aircraft-bucket-{account}-{region}`
    - Block public access
    - RemovalPolicy: DESTROY
    - AutoDeleteObjects: true

- **Lambda (container image)** `AircraftApiLambda`
    - VPC private isolated
    - Common DB env like Flights (AIRCRAFT\_\* keys)
    - Also configured with:
        - `AIRCRAFT_S3__BucketName`
        - `AIRCRAFT_SNS__AircraftCreatedTopicArn`

- **API route:** `ANY /aircraft`
- Permissions:
    - Connect to RDS Proxy (IAM auth + SG rule)
    - Publish to `AircraftCreatedTopic`
    - Read from S3 bucket (note: code grants **read**, not write)

**Event consumption:** Aircraft reacts to flight lifecycle topics via handler Lambdas:

- `AircraftFlightArrivedHandlerLambda` ← `FlightArrivedTopic`
- `AircraftFlightMarkedAsDelayedEnRouteHandlerLambda` ← `FlightMarkedAsDelayedEnRouteTopic`
- `AircraftFlightMarkedAsEnRouteHandlerLambda` ← `FlightMarkedAsEnRouteTopic`

---

### Event handling pattern (SNS → SQS → Lambda) and failure behavior

The `EventHandlerLambda` construct standardizes event-driven consumers:

- Creates a dedicated **handler Lambda** (container image) in private isolated subnet
- Subscribes an **SQS queue** to the SNS topic (`SqsSubscription`)
- Connects the queue to the Lambda via **SQS event source mapping**
    - Batch size: 10
    - `ReportBatchItemFailures = true` (partial failure reporting)

- Adds a **DLQ** with `MaxReceiveCount = 3`
- Grants the Lambda permission to consume from the queue
- Grants DB proxy connect and opens SG egress to proxy SG

**Why this matters:**

- SNS → SQS provides buffering and retry semantics.
- After 3 failed receives, messages land in the DLQ for investigation/replay.
- Handler Lambdas can be independently scaled and deployed per event type.

---

### Security & isolation model

- All Lambdas (API and handlers) run in **private isolated subnets**.
- No NAT: services rely on VPC endpoints for AWS service access.
- DB access is strictly via **RDS Proxy SG** rules + IAM auth.
- S3 bucket blocks public access; bucket is environment-specific via account/region naming.

---

### Notable operational characteristics (as implied by CDK choices)

- Many resources are configured with **RemovalPolicy.DESTROY** (DynamoDB table, S3 bucket, DB instance). This is convenient for dev/test but risky for production unless changed.
- The ACM certificate is explicitly **retained**, preventing accidental loss of a validated cert on stack teardown.
- Using container-image Lambdas standardizes runtime packaging across services and handlers.
