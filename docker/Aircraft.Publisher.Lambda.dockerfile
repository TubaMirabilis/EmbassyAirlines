FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Aircraft.Publisher.Lambda/ /src/Aircraft.Publisher.Lambda/
COPY ./src/Aircraft.Core/ /src/Aircraft.Core/
COPY ./src/Aircraft.Infrastructure/ /src/Aircraft.Infrastructure/
COPY ./src/Shared/ /src/Shared/
COPY ./src/Shared.EntityFrameworkCore/ /src/Shared.EntityFrameworkCore/
COPY ./Directory.Packages.props /Directory.Packages.props
WORKDIR /src/Aircraft.Publisher.Lambda
RUN dotnet restore
RUN dotnet publish -c Release --os linux -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Aircraft.Publisher.Lambda"]
