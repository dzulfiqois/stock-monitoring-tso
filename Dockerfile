# multi-stage: sdk for build → aspnet for runtime (non-root)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/StockMonitorTso.Domain/StockMonitorTso.Domain.csproj ./src/StockMonitorTso.Domain/
COPY src/StockMonitorTso.Infrastructure/StockMonitorTso.Infrastructure.csproj ./src/StockMonitorTso.Infrastructure/
COPY src/StockMonitorTso.Api/StockMonitorTso.Api.csproj ./src/StockMonitorTso.Api/
COPY src/StockMonitorTso.Web/StockMonitorTso.Web.csproj ./src/StockMonitorTso.Web/
RUN dotnet restore src/StockMonitorTso.Web/StockMonitorTso.Web.csproj

COPY . .
RUN dotnet publish src/StockMonitorTso.Web/StockMonitorTso.Web.csproj -c Release -o /app/publish --no-restore \
    && cp "Monitoring Tabung RPM(1).xlsx" /app/publish/ \
    && cp -r seeds /app/publish/seeds

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# siapkan user non-root + folder data/certs + self-signed cert untuk HTTPS PoC
USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends openssl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/data /app/certs \
    && openssl req -x509 -newkey rsa:2048 -nodes -keyout /app/certs/aspnetapp.key -out /app/certs/aspnetapp.crt -days 365 -subj "/CN=localhost" \
    && openssl pkcs12 -export -out /app/certs/aspnetapp.pfx -inkey /app/certs/aspnetapp.key -in /app/certs/aspnetapp.crt -passout pass:poctso \
    && rm /app/certs/aspnetapp.key /app/certs/aspnetapp.crt \
    && chown -R 1654:1654 /app \
    && chmod 600 /app/certs/aspnetapp.pfx

USER 1654

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetapp.pfx
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=poctso

EXPOSE 8080
EXPOSE 8081

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
  CMD bash -c "exec 3<>/dev/tcp/127.0.0.1/8080 && echo OK >&3" || exit 1

ENTRYPOINT ["dotnet", "StockMonitorTso.Web.dll"]