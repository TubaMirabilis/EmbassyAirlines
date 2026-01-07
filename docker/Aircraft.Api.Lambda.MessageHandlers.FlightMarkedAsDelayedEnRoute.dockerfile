FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute/ /src/Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute/
COPY ./src/Aircraft.Core/ /src/Aircraft.Core/
COPY ./src/Aircraft.Infrastructure/ /src/Aircraft.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute
RUN dotnet restore
RUN dotnet publish -c Release -r linux-x64 --self-contained false --no-restore -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute::Aircraft.Api.Lambda.MessageHandlers.FlightMarkedAsDelayedEnRoute.Function::FunctionHandler"]
