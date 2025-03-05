FROM public.ecr.aws/lambda/dotnet:9

COPY ./bin/Release/net9.0/linux-x64/publish/* /var/task

CMD [ "Airports.Api.Lambda" ]