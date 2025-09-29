FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ./src/Aircraft.Api.Lambda/ /src/Aircraft.Api.Lambda/
COPY ./src/Shared/ /src/Shared/
WORKDIR /src/Aircraft.Api.Lambda
RUN dotnet restore
RUN dotnet publish -c Release --os linux -o /app/publish

FROM public.ecr.aws/lambda/dotnet:10-preview
WORKDIR /var/task
COPY --from=build /app/publish .
CMD ["Aircraft.Api.Lambda"]
