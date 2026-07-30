FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Flights.Migrations.Lambda/ /src/Flights.Migrations.Lambda/
COPY ./src/Flights.Core/ /src/Flights.Core/
COPY ./src/Flights.Infrastructure/ /src/Flights.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/Shared.Npgsql/ /src/Shared.Npgsql/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Flights.Migrations.Lambda
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -r linux-x64 --self-contained false --no-restore -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Flights.Migrations.Lambda::Flights.Migrations.Lambda.Function::FunctionHandler"]
