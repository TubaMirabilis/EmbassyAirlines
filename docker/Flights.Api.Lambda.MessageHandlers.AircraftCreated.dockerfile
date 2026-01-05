FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Flights.Api.Lambda.MessageHandlers.AircraftCreated/ /src/Flights.Api.Lambda.MessageHandlers.AircraftCreated/
COPY ./src/Flights.Core/ /src/Flights.Core/
COPY ./src/Flights.Infrastructure/ /src/Flights.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Flights.Api.Lambda.MessageHandlers.AircraftCreated
RUN dotnet restore
RUN dotnet publish -c Release --os linux -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Flights.Api.Lambda.MessageHandlers.AircraftCreated::Flights.Api.Lambda.MessageHandlers.AircraftCreated.Function::FunctionHandler"]
