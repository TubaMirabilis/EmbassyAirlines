FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Airports.Api.Lambda/ /src/Airports.Api.Lambda/
COPY ./src/Airports.Core/ /src/Airports.Core/
COPY ./src/Airports.Infrastructure/ /src/Airports.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/Shared.EntityFrameworkCore/ /src/Shared.EntityFrameworkCore/
COPY ./src/AWS.Aspire.ServiceDefaults/ /src/AWS.Aspire.ServiceDefaults/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Airports.Api.Lambda
RUN dotnet restore
RUN dotnet publish -c Release --os linux -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Airports.Api.Lambda"]
