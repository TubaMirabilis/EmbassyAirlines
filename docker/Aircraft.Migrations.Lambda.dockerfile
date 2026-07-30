FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Aircraft.Migrations.Lambda/ /src/Aircraft.Migrations.Lambda/
COPY ./src/Aircraft.Core/ /src/Aircraft.Core/
COPY ./src/Aircraft.Infrastructure/ /src/Aircraft.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/Shared.EntityFrameworkCore/ /src/Shared.EntityFrameworkCore/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Aircraft.Migrations.Lambda
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -r linux-x64 --self-contained false --no-restore -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Aircraft.Migrations.Lambda::Aircraft.Migrations.Lambda.Function::FunctionHandler"]
