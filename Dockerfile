# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
WORKDIR /src

# 1) Restore as its own cached layer — only re-runs when the csproj changes.
COPY CardDuel.ServerApi.csproj ./
RUN dotnet restore CardDuel.ServerApi.csproj

# 2) Copy the rest and publish without re-restoring.
COPY . .
RUN dotnet publish CardDuel.ServerApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN addgroup -g 1001 dotnet && adduser -D -u 1001 -G dotnet dotnet
COPY --from=build /app/publish .
USER dotnet
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=300s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/api/v1/health || exit 1
ENTRYPOINT ["dotnet", "CardDuel.ServerApi.dll"]
