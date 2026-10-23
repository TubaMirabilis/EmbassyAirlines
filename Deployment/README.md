# Deployment

The Deployment project is a C# AWS CDK project that provisions the infrastructure for the Embassy Airlines staging environment. The resultant resources form a system which uses event-driven architecture, Lambda functions, SNS topics, API Gateway, and PostgreSQL.

## High-level architecture

The deployment provisions a complete serverless backend for the Embassy Airlines staging environment. Incoming requests resolve through Route 53 to a custom API Gateway domain, where a shared HTTP API routes requests to the Lambda-backed service endpoints. All services share common infrastructure such as the networking layer, PostgreSQL database, and messaging components while remaining logically separated through independent CDK constructs.

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

Both the base route and a greedy proxy route (`/{proxy+}`) are registered for each service, allowing nested resource paths to be forwarded to the corresponding Lambda. All HTTP methods are supported.

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

- Airports database migration Lambda
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

- Aircraft database migration Lambda
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

### Flights

Deploys:

- Flights database migration Lambda
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

Each consumer queue has a visibility timeout of 180 seconds and a dead-letter queue with a 14-day retention period. Messages that repeatedly fail processing are moved to the dead-letter queue after the configured receive threshold of three. Consumer Lambdas process batches of up to ten messages and support partial batch failure reporting, allowing unsuccessful messages to be retried independently when handlers return the appropriate failure response.

---

## Monitoring and alerting

Each event consumer is provisioned with two CloudWatch alarms to detect message-processing failures and delays.

- **Dead-letter queue alarm:** Monitors the approximate number of visible messages in the dead-letter queue. The alarm triggers when at least one message is detected during a one-minute evaluation period.
- **Queue age alarm:** Monitors the approximate age of the oldest message in the consumer queue. The alarm triggers when the oldest message exceeds 300 seconds for three consecutive one-minute evaluation periods.

Both alarms publish notifications to a shared SNS alarm topic, which has an email subscription configured using the `alarmEmailAddress` CDK context value.

The email subscription must be confirmed by the recipient before notifications can be delivered.

Missing CloudWatch metric data is treated as non-breaching to avoid alarms being triggered when queues are inactive.

---

## Publisher pattern

Each service publishes domain events using the Transactional Outbox Pattern rather than emitting events directly from the API. When a business operation modifies application data, the corresponding domain event is written to an outbox table within the same database transaction. Scheduled Publisher Lambdas run every minute, retrieve unpublished events from the outbox, publish them to the appropriate SNS topics, and then mark them as published. This approach ensures that business data changes and their corresponding outbox records can be committed atomically. Publisher Lambdas subsequently deliver committed events to SNS, reducing the risk of inconsistencies between database state and published events. Because publishing and message delivery may be retried, consumers should be designed to handle duplicate events safely.

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
- larger deployment package limits than ZIP-based Lambdas
- easier local testing

Database-backed Lambda functions share a common DatabaseBackedDockerLambda CDK construct, which configures Docker image deployment, VPC networking, security groups, CloudWatch logging, X-Ray tracing, and IAM permissions for connecting through the RDS Proxy. HTTP API, event handler, publisher, and database migration Lambdas reuse this construct while retaining their own invocation mechanisms and timeout configurations.

---

## Security

The system has been designed to follow several good security practices:

- Lambdas run inside private isolated subnets.
- Separate security groups are created for different Lambda roles.
- Lambda security groups are configured to permit database connections through the RDS Proxy on the PostgreSQL port.
- IAM authentication is used for database connections.
- Database credentials come from generated Secrets Manager secrets.
- S3 bucket blocks all public access.
- API uses TLS with ACM certificates.

---

## Database migrations

Each service provisions a dedicated database migration Lambda using a CloudFormation custom resource.

Migrations are invoked during stack creation and relevant updates. The Docker image asset hash is included in the custom resource properties so that changes to the migration image can trigger another migration invocation.

API, publisher, and event handler Lambdas declare dependencies on their respective migration resources, ensuring migrations complete successfully before those Lambda resources are created or updated.

Migration handlers must support the CloudFormation custom resource lifecycle, including create, update, and delete events.

---

## Deployment configuration

The deployment requires an email address for operational alarm notifications. This is supplied through the `alarmEmailAddress` AWS CDK context value.

For example:

```bash
cdk deploy -c alarmEmailAddress=alerts@example.com
```

The deployment fails during CDK stack construction if this value is missing or empty.

After deployment, the recipient must confirm the SNS email subscription to receive CloudWatch alarm notifications.

---

## Resource lifecycle

The current deployment is configured for easy teardown rather than data retention. The PostgreSQL instance uses `RemovalPolicy.DESTROY` with deletion protection disabled, and the Aircraft S3 bucket is configured to delete its contents when the stack is removed. These settings should be reviewed before using the stack for production workloads.
