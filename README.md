# EmbassyAirlines

This represents a cloud-native airline management system with proper testing, deployment automation, and microservices best practices.

## Project Structure & Infrastructure

- **Multi-service architecture** with 3 main APIs and shared libraries
- **AWS CDK deployment** configuration for cloud infrastructure
- **Docker containerization** for each service
- **GitHub Actions CI/CD pipeline** with build, test, and deployment stages

## Core Services

### 1. **Aircraft API (Lambda)**

- Manages aircraft fleet with seat configurations
- **PostgreSQL database** with Entity Framework Core
- **S3 integration** for seat layout definitions (JSON files)
- **SNS publishing** for aircraft creation events
- Complex seat layout system with business/economy configurations

### 2. **Airports API (Lambda)**

- Airport management with IATA/ICAO codes and timezone handling
- **DynamoDB storage** with custom repository pattern
- **SNS publishing** for airport created/updated events
- Full CRUD operations with validation

### 3. **Flights API (Web App)**

- Flight scheduling and management system
- **PostgreSQL database** with NodaTime for timezone-aware scheduling
- **Event-driven architecture** consuming aircraft/airport events via SQS
- Flight operations: scheduling, rescheduling, aircraft assignment, pricing adjustments

## Technical Features

- **Shared library** with common contracts, validation, error handling, and middleware
- **Comprehensive validation** using FluentValidation
- **Error handling** with ErrorOr pattern and standardized problem details
- **Structured logging** with Serilog and correlation IDs
- **Extensive functional tests** with test containers (PostgreSQL, DynamoDB, LocalStack)
- **Code quality enforcement** with EditorConfig, analyzers, and formatting rules

## Architecture Patterns

- **Domain-driven design** with rich domain models
- **Event-driven communication** between services
- **CQRS-style** separation with distinct read/write operations
- **Repository pattern** for data access abstraction
- **Middleware pipeline** for cross-cutting concerns
