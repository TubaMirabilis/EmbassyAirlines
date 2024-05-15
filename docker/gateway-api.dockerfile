# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0.300 AS build
COPY ./src/Gateway.Api/ /src/Gateway.Api/
COPY ./src/Shared/ /src/Shared/

WORKDIR /src/Gateway.Api
RUN dotnet restore "Gateway.Api.csproj"
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
# WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
