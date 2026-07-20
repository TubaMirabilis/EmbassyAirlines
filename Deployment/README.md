# Deployment

The Deployment project is a C# AWS CDK project that provisions the infrastructure for the Embassy Airlines staging environment. The resultant resources form a system which uses event-driven architecture, Lambda functions, SNS topics, API Gateway, and PostgreSQL.

## Infrastructure components

### 1. Networking

A dedicated VPC is created with:

- 2 Availability Zones
- No NAT Gateways
- Private isolated subnets
- VPC endpoints for:

    - S3
    - SNS
    - SQS
    - DynamoDB

This design allows the Lambdas to remain inside private subnets while still reaching AWS services without internet access.

---

### 2. Shared API

The SharedInfra CDK construct provisions the shared infrastructure required to expose the various HTTP APIs over a custom domain.

It imports the existing Route 53 hosted zone for embassyairlines.com, creates an ACM certificate validated via DNS, configures an API Gateway custom domain, and maps the HTTP API under the /api base path. Every service adds routes onto this single API.

Finally, it creates a Route 53 alias record so that requests to https://embassyairlines.com/api are routed to the API Gateway endpoint.

---

### 3. Messaging

The project provisions many SNS topics representing business events:

- AircraftCreated
- AirportCreated
- AirportUpdated
- FlightScheduled
- FlightCancelled
- FlightDelayed
- FlightArrived
- FlightRescheduled
- FlightPricingAdjusted
- etc.

These topics form the communication backbone between services.

---

### 4. Database

Unlike the Airports service, which uses DynamoDB, the Aircraft and Flights services share:

- PostgreSQL RDS
- RDS Proxy
- IAM authentication
- generated Secrets Manager credentials

The Lambdas never connect directly to PostgreSQL—they connect through the RDS Proxy.

The Aircraft and Flights services currently share a single PostgreSQL database. In a production environment, each service would typically own its own database to maintain stronger service boundaries and independent scalability. However, provisioning separate RDS instances would significantly increase the infrastructure cost for a demonstration project while providing little additional value for showcasing the architecture. The services remain logically separated, making it straightforward to split them into independent databases if required.

---

## Services

### Airports Service

This service is comparatively simple.

It provisions:

- DynamoDB table (`airports`)
- API Lambda
- publishes AirportCreated events
- publishes AirportUpdated events

No PostgreSQL is involved.

---

### Aircraft Service

This service has a broader infrastructure footprint than the Airports service.

Infrastructure includes:

- API Lambda
- S3 bucket
- Event handlers
- Publisher Lambda
- PostgreSQL access

It listens for events like:

- FlightArrived
- FlightMarkedAsEnRoute
- FlightMarkedAsDelayedEnRoute

It publishes:

- AircraftCreated

It also owns an S3 bucket for aircraft assets or documents.

---

### Flights Service

This is the largest service.

It contains:

- API Lambda
- Event handlers
- Publisher Lambda

It consumes:

- AircraftCreated
- AirportCreated
- AirportUpdated

It publishes many events:

- FlightScheduled
- AircraftAssignedToFlight
- FlightPricingAdjusted
- FlightCancelled
- FlightDelayed
- FlightArrived
- FlightRescheduled
- etc.

---

## Lambda patterns

The project defines three reusable infrastructure constructs.

### HTTP Lambda

Creates:

- Docker image Lambda
- API Gateway route
- database connectivity
- security group

Every REST endpoint is built using this abstraction.

---

### Event Handler Lambda

Creates:

- Docker Lambda
- SQS queue
- Dead-letter queue
- SNS subscription
- SQS event source

---

### Publisher Lambda

Publisher Lambdas run on a one-minute schedule and publish pending domain events from the transactional outbox, ensuring events are only emitted after the associated database transaction has completed successfully.

---

## Deployment style

Every Lambda is deployed as a Docker container image, using Dockerfiles such as:

- `Aircraft.Api.Lambda.dockerfile`
- `Flights.Api.Lambda.dockerfile`
- `Aircraft.Publisher.Lambda.dockerfile`
- message handler Dockerfiles
