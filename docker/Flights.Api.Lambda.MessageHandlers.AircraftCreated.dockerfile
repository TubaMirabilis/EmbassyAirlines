FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Flights.Api.Lambda.MessageHandlers.AircraftCreated/ /src/Flights.Api.Lambda.MessageHandlers.AircraftCreated/
COPY ./src/Flights.Core/ /src/Flights.Core/
COPY ./src/Flights.Infrastructure/ /src/Flights.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/Shared.Messaging/ /src/Shared.Messaging/
COPY ./src/Shared.Npgsql/ /src/Shared.Npgsql/
COPY ./src/Shared.ServiceDefaults/ /src/Shared.ServiceDefaults/
COPY ./src/Shared.TransactionalOutbox/ /src/Shared.TransactionalOutbox/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Flights.Api.Lambda.MessageHandlers.AircraftCreated
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -r linux-x64 --self-contained false --no-restore -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10

WORKDIR /var/task

COPY --from=build /app/publish .

# Install AWS ADOT Collector Lambda extension
COPY docker/observability/opt/ /opt/

# Ensure the Linux extension is executable
RUN chmod 755 /opt/extensions/collector

CMD ["Flights.Api.Lambda.MessageHandlers.AircraftCreated::Flights.Api.Lambda.MessageHandlers.AircraftCreated.Function::FunctionHandler"]
