FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY StockMonitorTso.sln ./
COPY src/StockMonitorTso.Domain/StockMonitorTso.Domain.csproj src/StockMonitorTso.Domain/
COPY src/StockMonitorTso.Infrastructure/StockMonitorTso.Infrastructure.csproj src/StockMonitorTso.Infrastructure/
COPY src/StockMonitorTso.Api/StockMonitorTso.Api.csproj src/StockMonitorTso.Api/
RUN dotnet restore src/StockMonitorTso.Api/StockMonitorTso.Api.csproj -r linux-musl-x64

COPY src/ src/
RUN dotnet publish src/StockMonitorTso.Api/StockMonitorTso.Api.csproj -c Release -o /app/publish --no-restore -r linux-musl-x64 --self-contained false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# QuestPDF (Skia musl) butuh fontconfig + font untuk render teks; /app/keys milik user non-root.
RUN apk add --no-cache fontconfig font-dejavu \
    && mkdir -p /app/keys \
    && chown -R 1654:1654 /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build --chown=1654:1654 /app/publish .
COPY --chown=1654:1654 seeds ./seeds
USER 1654
ENTRYPOINT ["dotnet", "StockMonitorTso.Api.dll"]
