# Base Image
From mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Add api user and group
RUN groupadd -r api && useradd -r -g api api

# Builder Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy API layers for building
COPY ./src/EmbassyAirlines.Api/ /src/EmbassyAirlines.Api/
COPY ./src/EmbassyAirlines.Application/ /src/EmbassyAirlines.Application/
COPY ./src/EmbassyAirlines.Infrastructure/ /src/EmbassyAirlines.Infrastructure/
COPY ./src/EmbassyAirlines.Domain/ /src/EmbassyAirlines.Domain/
COPY ./src/EaCommon/ /src/EaCommon/

WORKDIR /src/EmbassyAirlines.Api
RUN dotnet restore "EmbassyAirlines.Api.csproj"

# Publish
RUN dotnet publish "EmbassyAirlines.Api.csproj" -c Release -o /app/publish

# Final Image
FROM base AS final
WORKDIR /app

# Copy the binaries from the build image
WORKDIR /app
COPY --from=build /app/publish .

# Entrypoint
ENTRYPOINT ["dotnet", "EmbassyAirlines.Api.dll"]
