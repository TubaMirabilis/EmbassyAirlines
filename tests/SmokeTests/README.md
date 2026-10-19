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
8. Marks the flight as en-route.
9. Verifies that the aircraft's status is updated to `EnRoute` once the event propagates to the Aircraft service.

---

## Prerequisites

### .NET SDK

- The **.NET 10 SDK** (the project targets `net10.0`). Check with `dotnet --list-sdks`.
- `dotnet run` restores and builds the project and its reference to `src/Shared`, so no separate build step is needed.

### A live deployment

- `EmbassyAirlinesStack` deployed with `cdk deploy` (see [`Deployment/README.md`](../../Deployment/README.md)), with its API reachable at the base URL you pass. The APIs are unauthenticated, so the HTTP calls need no credentials.
- A deployment the smoke test has not already run against. The test creates fixed resources (airports `ICN` and `AMS`, aircraft `PH-JRN`) and there are no delete endpoints, so a second run fails with `409 Conflict` when it tries to create the first airport.

### AWS CLI

The program shells out to `aws` (via CliWrap) to discover the account ID and region, so you need:

- **AWS CLI v2**, installed and on your `PATH` as `aws`.
- A **region set in the AWS config file** for the active profile, matching the region the stack is deployed to (`eu-west-2`). The program reads it with `aws configure get region`, which only reads the config file and ignores the `AWS_REGION` and `AWS_DEFAULT_REGION` environment variables:

    ```bash
    aws configure set region eu-west-2
    ```

- `aws sts get-caller-identity` returning the account the stack is deployed to.

If you use a named profile, select it with the `AWS_PROFILE` environment variable rather than `--profile`. The program passes no profile to either the CLI or the SDK, so `AWS_PROFILE` is the only way to make both use it.

### AWS credentials

The S3 upload uses the AWS SDK for .NET (`AmazonS3Client`), which resolves credentials and region on its own, separately from the CLI. Both must resolve to the same account and region; otherwise the bucket name built from the CLI's answers won't match the bucket the SDK calls.

- **Access keys** in environment variables (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, optionally `AWS_SESSION_TOKEN`) or in the shared credentials file work for both.
- **IAM Identity Center (SSO) and assume-role profiles** do not work for the S3 upload as-is. The SDK needs extra packages to resolve them (`AWSSDK.SSO` and `AWSSDK.SSOOIDC`, or `AWSSDK.SecurityToken`), and this project references only `AWSSDK.S3`. Sign in with the CLI and export temporary credentials into your shell first:

    ```powershell
    # PowerShell
    aws configure export-credentials --format powershell | Invoke-Expression
    ```

    ```bash
    # bash / zsh
    eval "$(aws configure export-credentials --format env)"
    ```

### AWS permissions

The identity only needs to write the seat layout to the Aircraft bucket:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": "s3:PutObject",
            "Resource": "arn:aws:s3:::aircraft-bucket-<account-id>-eu-west-2/seat-layouts/*"
        }
    ]
}
```

- `sts:GetCallerIdentity` needs no policy; AWS always allows it.
- The bucket uses default S3-managed encryption, so no KMS permissions are needed.

---

## Running the smoke tests

Run from the **repository root**. The seat layout path `Resources/Layouts/B78X.json` is resolved relative to the current directory, and the file lives at the repository root, not in this project.

```bash
dotnet run --project tests/SmokeTests -- https://embassyairlines.com/api/
```

Omit the URL to use the default, `https://embassyairlines.com/api/`. Keep the trailing slash on any URL you pass: endpoint paths such as `airports` are resolved relative to the base URL, so without it the final segment (`api`) is dropped.

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

Before interacting with each service, the application performs a GET request against its corresponding API endpoint:

- `/airports`
- `/aircraft`
- `/flights`

Each readiness check expects an HTTP `200 OK` response.

If an endpoint returns any other HTTP status code, execution stops with an exception.

These checks provide an initial indication that the API endpoints are responding before the smoke test attempts to create resources. They do not continuously poll or wait for services to become ready.

---

## Step 3 – Create airports

Two airports are created:

| Airport                       | IATA | Time Zone        |
| ----------------------------- | ---- | ---------------- |
| Incheon International Airport | ICN  | Asia/Seoul       |
| Schiphol Airport              | AMS  | Europe/Amsterdam |

Each POST:

- sends JSON
- verifies success
- deserializes the returned object
- logs how long the request took

This is a typical pattern used throughout the application.

---

## Step 4 – Upload aircraft configuration to S3

Before creating the aircraft, the program uploads a seat-layout JSON file.

It:

- reads `Resources/Layouts/B78X.json`
- discovers the current AWS account ID using the AWS CLI
- discovers the configured AWS region
- uploads the JSON to an S3 bucket named like:

```
aircraft-bucket-{accountId}-{region}
```

using the key:

```
seat-layouts/B78X.json
```

It uses:

- AWS SDK (`AmazonS3Client`)
- AWS CLI
- CliWrap

If the upload fails, an exception is thrown.

---

## Step 5 – Create an aircraft

The application creates an aircraft with data including:

- Tail number (`PH-JRN`)
- Aircraft type (`B78X`)
- Status
- Current airport
- Operational limits such as weights

The aircraft API returns an `AircraftDto`, whose ID is then used when scheduling the flight.

---

## Step 6 – Calculate flight times

Instead of hard-coding dates, the application calculates departure and arrival times dynamically using the **NodaTime** library.

It:

- Calculates a target departure time **30 minutes from the current time**.
- Rounds the departure time forward to the next whole-minute boundary.
- Uses a flight duration of **10 hours and 30 minutes**.
- Calculates the arrival time by adding the flight duration to the departure instant.
- Converts departure and arrival instants into the respective airports' local time zones using NodaTime.
- Converts the resulting zoned times into local `DateTime` values for the scheduling request.

This ensures that departure and arrival times account for the different time zones of the airports, including applicable daylight saving time rules.

---

## Step 7 – Schedule a flight

A `ScheduleFlightDto` is sent containing:

- aircraft ID
- departure airport
- arrival airport
- local departure time
- local arrival time
- economy fare
- business fare
- flight numbers
- operation type

This validates the complete scheduling workflow.

---

## Step 8 – Mark the flight as en-route

After successfully scheduling a flight, the application verifies that its status can be updated through the Flights API.

It performs an HTTP PATCH request using the endpoint:

`PATCH /flights/{id}/status`

The request uses `AdjustFlightStatusDto` and marks the flight as en-route.

The request:

- Sends the required status as JSON.
- Verifies that the API returns a successful HTTP status code.
- Logs the time taken to complete the operation.
- Logs the flight ID and requested status.

If a status update fails, the smoke test stops with an exception.

This extends the end-to-end smoke test to cover flight status transitions after scheduling.

---

## Step 9 – Verify aircraft status propagation

After marking the flight as en-route, the application verifies that the corresponding aircraft status has been updated by the Aircraft service.

The Flights service publishes a `FlightMarkedAsEnRouteEvent`, which is consumed by the Aircraft service to update the aircraft's status.

The smoke test sends an HTTP GET request to:

`GET /aircraft/{id}`

It deserializes the response into an `AircraftDto` and examines the `Status` property.

The expected aircraft status is `EnRoute`.

Because event propagation is asynchronous, the aircraft may initially retain its previous status. The smoke test therefore uses a retry policy:

- Up to **180 retries** after the initial request (**181 attempts in total**).
- A **fixed delay of 1 second** between attempts.
- Retries when the returned aircraft status does not match `EnRoute`.
- Stops immediately when the expected status is observed.
- Throws an exception if the expected status is not observed after the maximum number of attempts.
- Supports cancellation through a `CancellationToken`.

Successful completion verifies that the aircraft's status has been updated following the flight status transition, providing end-to-end coverage of the event-driven workflow.

---

## Retry behavior

The Embassy Airlines backend relies on eventual consistency. Therefore, newly created airports or aircraft may not be immediately visible to the flight service.

When scheduling a flight, the SmokeTests project retries HTTP POST requests that return a `404 Not Found` status code, allowing time for newly created resources to become available.

The retry policy is configured as follows:

- Up to **180 retries** after the initial request (**181 attempts in total**).
- A **fixed delay of 1 second** between retries.
- Only HTTP `404 Not Found` responses trigger a retry.
- Other HTTP responses are returned immediately and unsuccessful status codes cause an exception.
- If the maximum number of retries is reached, the final response is returned and the failure is logged.
- Retry operations support cancellation through a `CancellationToken`.

The maximum retry delay is approximately **3 minutes**, excluding the time taken to execute HTTP requests.

This approach allows the smoke test to tolerate temporary resource-visibility delays caused by eventual consistency without immediately failing.

---

## Design characteristics

The SmokeTests project is organized into small, focused components:

- `ServiceReadiness` – checks APIs are available.
- `AirportsSmokeTestActions` – manages airports.
- `AircraftSmokeTestActions` – manages aircraft and uploads seat layouts.
- `FlightsSmokeTestActions` – manages flights using retry logic when appropriate.
- `FlightTimeCalculator` – computes time-zone-aware departure and arrival times.
