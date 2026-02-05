FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Flights.Api.Lambda.MessageHandlers.AirportCreated/ /src/Flights.Api.Lambda.MessageHandlers.AirportCreated/
COPY ./src/Flights.Core/ /src/Flights.Core/
COPY ./src/Flights.Infrastructure/ /src/Flights.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/AWS.Aspire.ServiceDefaults/ /src/AWS.Aspire.ServiceDefaults/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Flights.Api.Lambda.MessageHandlers.AirportCreated
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -r linux-x64 --self-contained false --no-restore -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Flights.Api.Lambda.MessageHandlers.AirportCreated::Flights.Api.Lambda.MessageHandlers.AirportCreated.Function::FunctionHandler"]
