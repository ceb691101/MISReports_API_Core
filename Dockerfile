# MIS Reports API (.NET 10) on Linux.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MISReports.Api.csproj .
RUN dotnet restore -p:InformixDriver=Db2
COPY . .
RUN dotnet publish -c Release -o /app --no-restore -p:InformixDriver=Db2

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Java runtime for the Jasper report tools; libxml2 for IBM's Informix/Db2 client library.
RUN apt-get update \
 && apt-get install -y --no-install-recommends openjdk-21-jre-headless libxml2 \
 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# IBM's native client ships inside the NuGet package (publish output folder "clidriver").
ENV LD_LIBRARY_PATH=/app/clidriver/lib \
    ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
# Logs (NLog daily files) and the Jasper tools are mounted from the host.
RUN mkdir -p /app/logs /app/JasperTools /app/jrxmls /app/secrets && chown -R app:app /app/logs
VOLUME ["/app/logs"]
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "MISReports.Api.dll"]
