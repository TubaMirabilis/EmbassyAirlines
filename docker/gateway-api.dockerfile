# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY ./src/Gateway.Api/ /src/Gateway.Api/

WORKDIR /src/Gateway.Api
RUN dotnet restore "Gateway.Api.csproj"
RUN dotnet build "Gateway.Api.csproj" -c Release -o /app/build --no-restore
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish --no-restore --no-build

# Final image
FROM base AS final
# WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
