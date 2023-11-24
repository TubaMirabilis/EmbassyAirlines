# Base Image
From mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Add sso user and group
RUN groupadd -r sso && useradd -r -g sso sso

# Builder Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy API layers for building
COPY ./src/EaIdentity.Api/ /src/EaIdentity.Api/
COPY ./src/EaIdentity.Application/ /src/EaIdentity.Application/
COPY ./src/EaIdentity.Domain/ /src/EaIdentity.Domain/
COPY ./src/EaIdentity.Infrastructure/ /src/EaIdentity.Infrastructure/
COPY ./src/EaCommon/ /src/EaCommon/

WORKDIR /src/EaIdentity.Api
RUN dotnet restore "EaIdentity.Api.csproj"

# Publish
RUN dotnet publish "EaIdentity.Api.csproj" -c Release -o /app/publish

# Final Image
FROM base AS final
WORKDIR /app

# Copy the binaries from the build image
COPY --from=build /app/publish .

# Entrypoint
ENTRYPOINT ["dotnet", "EaIdentity.Api.dll"]