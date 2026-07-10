# Smoke Tests

The Smoke Tests project provides automated end-to-end smoke tests for the Embassy Airlines backend. It exercises the various REST APIs to verify that critical application workflows are functioning correctly.

When the application starts, it:

1. Connects to an API (defaulting to `https://embassyairlines.com/api/` unless another URL is supplied as a command-line argument).
2. Verifies that each service is available.
3. Creates two airports.
4. Uploads an aircraft seat layout to Amazon S3.
5. Creates an aircraft.
6. Calculates realistic departure and arrival times.
7. Schedules a flight using the newly created resources. 

---

## Step 1 – Configure the API

The program creates a single `HttpClient` with a configurable base URL:

```csharp
var baseAddress = args.Length > 0
    ? args[0]
    : "https://embassyairlines.com/api/";
```

This makes it easy to run against different environments.

---

## Step 2 – Check service readiness

Before using each microservice, the application performs a simple GET request.

For example:

* `/airports`
* `/aircraft`
* `/flights`

If any service doesn't return HTTP 200, execution stops with an exception.

This prevents later failures caused simply by a service not having finished starting up. 

---

## Step 3 – Create airports

Two airports are created:

| Airport                       | IATA | Time Zone        |
| ----------------------------- | ---- | ---------------- |
| Incheon International Airport | ICN  | Asia/Seoul       |
| Schiphol Airport              | AMS  | Europe/Amsterdam |

Each POST:

* sends JSON
* verifies success
* deserializes the returned object
* logs how long the request took

This is a typical pattern used throughout the application.  

---

## Step 4 – Upload aircraft configuration to S3

Before creating the aircraft, the program uploads a seat-layout JSON file.

It:

* reads `Resources/Layouts/B78X.json`
* discovers the current AWS account ID using the AWS CLI
* discovers the configured AWS region
* uploads the JSON to an S3 bucket named like:

```
aircraft-bucket-{accountId}-{region}
```

using the key:

```
seat-layouts/B78X.json
```

It uses:

* AWS SDK (`AmazonS3Client`)
* AWS CLI
* CliWrap

If the upload fails, an exception is thrown.  

---

## Step 5 – Create an aircraft

The application creates an aircraft with data including:

* Tail number (`PH-JRN`)
* Aircraft type (`B78X`)
* Status
* Current airport
* Operational limits such as weights

The aircraft API returns an `AircraftDto`, whose ID is then used when scheduling the flight.  

---

## Step 6 – Calculate flight times

Instead of hard-coding dates, the application computes realistic times.

It:

* schedules departure **30 minutes from now**
* uses a **10 hour 30 minute** flight duration
* converts the times into the departure and arrival airports' local time zones using the **NodaTime** library

This ensures the scheduled flight uses correct local times despite differing time zones.  

---

## Step 7 – Schedule a flight

Finally it sends a `ScheduleFlightDto` containing:

* aircraft ID
* departure airport
* arrival airport
* local departure time
* local arrival time
* economy fare
* business fare
* flight numbers
* operation type

This validates the complete scheduling workflow. 

---

## Retry behaviour

The Embassy Airlines backend relies on eventual consistency. Therefore newly created airports or aircraft may not be immediately visible to the flight service.

The SmokeTests project has a smoke test for scheduling a flight via a HTTP request. If the schedule request fails with a Not Found HTTP status code, the request is retried in accordance with the provided retry policy:

* up to 5 retries
* exponential backoff
* 1 second initial delay

---

## Design characteristics

The SmokeTests project is organised into small, focused components:

* `ServiceReadiness` – checks APIs are available.
* `AirportsSmokeTestActions` – manages airports.
* `AircraftSmokeTestActions` – manages aircraft and uploads seat layouts.
* `FlightsSmokeTestActions` – manages flights using retry logic when appropriate.
* `FlightTimeCalculator` – computes time-zone-aware departure and arrival times.
