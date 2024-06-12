# Base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0.302 AS build
COPY ./src/Fleet.Api/ /src/Fleet.Api/
COPY ./src/Shared/ /src/Shared/

WORKDIR /src/Fleet.Api
RUN dotnet restore "Fleet.Api.csproj"
RUN dotnet publish "Fleet.Api.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
# WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fleet.Api.dll"]
