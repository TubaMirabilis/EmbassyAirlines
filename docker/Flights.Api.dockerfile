# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-noble AS base
WORKDIR /app

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
COPY ./src/Flights.Api/ /src/Flights.Api/
COPY ./src/Shared/ /src/Shared/
RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg
WORKDIR /src/Flights.Api
RUN dotnet restore "Flights.Api.csproj"
RUN dotnet publish "Flights.Api.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
# WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Flights.Api.dll"]
