FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Airports.Migrations.Lambda/ /src/Airports.Migrations.Lambda/
COPY ./src/Airports.Core/ /src/Airports.Core/
COPY ./src/Airports.Infrastructure/ /src/Airports.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/Shared.Npgsql/ /src/Shared.Npgsql/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Airports.Migrations.Lambda
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -r linux-x64 --self-contained false --no-restore -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Airports.Migrations.Lambda::Airports.Migrations.Lambda.Function::FunctionHandler"]
