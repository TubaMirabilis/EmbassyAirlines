# Deployment

The Deployment project is a C# AWS CDK project that provisions the infrastructure for the Embassy Airlines staging environment. The resultant resources form a system which uses event-driven architecture, Lambda functions, SNS topics, API Gateway, and PostgreSQL.

## High-level architecture

The deployment provisions a complete serverless backend for the Embassy Airlines staging environment. Incoming requests are routed through a custom Route 53 domain to a shared API Gateway HTTP API, where each business domain exposes its own set of Lambda-backed endpoints. All services share common infrastructure such as the networking layer, PostgreSQL database, and messaging components while remaining logically separated through independent CDK constructs.

Application data is stored in PostgreSQL and accessed exclusively through an RDS Proxy, allowing Lambda functions to scale without overwhelming the database with large numbers of concurrent connections. Changes that produce domain events are first recorded in a transactional outbox table. Scheduled publisher Lambdas then publish those events to SNS topics, ensuring events are only emitted after the associated database transaction succeeds. Consumer services receive events through dedicated SQS queues, allowing each service to process messages independently with retries, dead-letter queues, and batch processing.

---

## Main infrastructure components

### 1. Networking

The `Network` construct creates:

- a VPC
- 2 Availability Zones
- **no NAT gateways**
- S3 Gateway Endpoint
- SQS Interface Endpoint
- SNS Interface Endpoint

All Lambda functions run in private isolated subnets. Therefore VPC endpoints are needed to provide access to required AWS services (S3, SNS and SQS). There is no requirement for Internet access or NAT Gateways.

---

### 2. Database

The project provisions a PostgreSQL RDS instance.

Characteristics:

- PostgreSQL 18.3
- t4g.micro
- 20 GB storage
- private isolated subnet
- generated Secrets Manager credentials
- RDS Proxy enabled
- IAM authentication enabled

Every Lambda connects through the **RDS Proxy**, rather than directly to PostgreSQL.

Lambda functions can scale rapidly, potentially creating large numbers of concurrent database connections. The RDS Proxy pools and reuses connections, reducing connection overhead and protecting PostgreSQL from connection exhaustion.

---

### 3. API Gateway

A shared HTTP API is created once.

Each service registers its own routes on the shared HTTP API:

```
/airports
/flights
/aircraft
```

Each route points to its own Docker Lambda.

A custom domain is also configured:

```
embassyairlines.com
```

using:

- ACM certificate
- Route53 Hosted Zone
- API Gateway domain mapping

The API is exposed under the `api` mapping key.

---

## The three business services

The infrastructure clearly separates business domains.

### Airports

Deploys:

- Airports API Lambda
- Airports Publisher Lambda

Publishes:

- AirportCreated
- AirportUpdated

Consumes:

- nothing

---

### Aircraft

Deploys:

- Aircraft API
- Publisher
- FlightArrived handler
- FlightMarkedAsEnRoute handler
- FlightMarkedAsDelayedEnRoute handler
- S3 bucket for aircraft data/images

Consumes flight lifecycle events.

Publishes:

- AircraftCreated

---

## Flights

Deploys:

- Flights API
- Publisher
- AircraftCreated handler
- AirportCreated handler
- AirportUpdated handler

Publishes the following domain events:

- FlightScheduled
- AircraftAssignedToFlight
- FlightPricingAdjusted
- FlightRescheduled
- FlightCancelled
- FlightDelayed
- FlightMarkedAsEnRoute
- FlightMarkedAsDelayedEnRoute
- FlightArrived

---

## Event-driven architecture

Rather than subscribing Lambda functions directly to SNS topics, every event consumer is provisioned with its own dedicated SQS queue. SNS fans events out to the appropriate queues, while each queue triggers its corresponding Lambda function. This architecture provides several advantages over direct SNS-to-Lambda integration, including durable message storage, automatic retry behaviour, dead-letter queues for failed messages, independent scaling of consumers, and the ability for services to process events at their own pace without affecting publishers.

---

## Publisher pattern

Each service publishes domain events using the Transactional Outbox Pattern rather than emitting events directly from the API. When a business operation modifies application data, the corresponding domain event is written to an outbox table within the same database transaction. Scheduled Publisher Lambdas run every minute, retrieve unpublished events from the outbox, publish them to the appropriate SNS topics, and then mark them as published. This approach guarantees that events are only emitted after the associated database transaction has completed successfully, preventing inconsistencies between the application's persistent state and the events observed by downstream services.

---

## Docker-based Lambdas

Every Lambda is packaged as a Docker image.

Examples include:

```
Aircraft.Api.Lambda.dockerfile
Flights.Api.Lambda.dockerfile
Flights.Publisher.Lambda.dockerfile
...
```

Advantages:

- native dependencies
- consistent builds
- no Lambda size limitations
- easier local testing

---

## Security

The system has been designed to follow several good security practices:

- Lambdas run inside private isolated subnets.
- Separate security groups are created for different Lambda roles.
- Only the RDS Proxy is reachable by the Lambdas.
- IAM authentication is used for database connections.
- Database credentials come from generated Secrets Manager secrets.
- S3 bucket blocks all public access.
- API uses TLS with ACM certificates.
