FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Airports.Api.Lambda/ /src/Airports.Api.Lambda/
COPY ./src/Airports.Core/ /src/Airports.Core/
COPY ./src/Airports.Infrastructure/ /src/Airports.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/Shared.HttpApiLambdaDefaults/ /src/Shared.HttpApiLambdaDefaults/
COPY ./src/Shared.Npgsql/ /src/Shared.Npgsql/
COPY ./src/Shared.ServiceDefaults/ /src/Shared.ServiceDefaults/
COPY ./src/Shared.TransactionalOutbox/ /src/Shared.TransactionalOutbox/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Airports.Api.Lambda
RUN dotnet restore
RUN dotnet publish -c Release --os linux -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10

WORKDIR /var/task

COPY --from=build /app/publish .

# Install AWS ADOT Collector Lambda extension
COPY docker/observability/opt/ /opt/

# Ensure the Linux extension is executable
RUN chmod 755 /opt/extensions/collector

CMD ["Airports.Api.Lambda"]
