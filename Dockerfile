# syntax=docker/dockerfile:1

# Multi-stage build for the BookingSystem API and its database migrator.
#
#   docker build -t bookingsystem-api .                              # API (default, last stage)
#   docker build -t bookingsystem-migrator --target migrator .       # EF Core migration bundle
#
# It lives at the repository root because the build needs root-level files (Directory.Build.props,
# nuget.config, the local tool manifest) next to src/, and `docker build .` stays the obvious command.

ARG DOTNET_VERSION=10.0

# ---------------------------------------------------------------------------------------------
# build: restore (cached layer) and publish the API.
# Runs on the build machine's architecture and cross-targets TARGETARCH (amd64 or arm64).
# ---------------------------------------------------------------------------------------------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS build
ARG TARGETARCH
WORKDIR /src

RUN case "$TARGETARCH" in \
      amd64) echo linux-x64 ;; \
      arm64) echo linux-arm64 ;; \
      *) echo "Unsupported TARGETARCH: $TARGETARCH" >&2; exit 1 ;; \
    esac > /tmp/rid

# Only the files that affect restore, so this layer is reused until a project file or package changes.
COPY Directory.Build.props nuget.config .editorconfig ./
COPY .config/dotnet-tools.json .config/
COPY src/BookingSystem.Domain/BookingSystem.Domain.csproj src/BookingSystem.Domain/
COPY src/BookingSystem.Application/BookingSystem.Application.csproj src/BookingSystem.Application/
COPY src/BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj src/BookingSystem.Infrastructure/
COPY src/BookingSystem.Api/BookingSystem.Api.csproj src/BookingSystem.Api/
RUN dotnet restore src/BookingSystem.Api/BookingSystem.Api.csproj --runtime "$(cat /tmp/rid)" \
    && dotnet tool restore

COPY src/ src/

# Framework-dependent: the runtime comes from the base image. No apphost: the entrypoint is `dotnet <dll>`.
RUN dotnet publish src/BookingSystem.Api/BookingSystem.Api.csproj \
      --configuration Release \
      --runtime "$(cat /tmp/rid)" \
      --self-contained false \
      --no-restore \
      --output /app/publish \
      -p:UseAppHost=false

# ---------------------------------------------------------------------------------------------
# bundle: single-file executable that applies pending migrations. It reads the connection string
# from BOOKINGSYSTEM_DB_CONNECTION (the design-time factory in Infrastructure) or from --connection.
# `dotnet ef` loads the design-time build for TARGETARCH into the build machine's process, so this
# stage only works when both architectures match: the migrator is built for one platform at a time
# (native locally; linux/amd64 when published by .github/workflows/docker.yml).
# ---------------------------------------------------------------------------------------------
FROM build AS bundle
RUN dotnet ef migrations bundle \
      --project src/BookingSystem.Infrastructure \
      --startup-project src/BookingSystem.Infrastructure \
      --configuration Release \
      --runtime "$(cat /tmp/rid)" \
      --output /app/efbundle

# ---------------------------------------------------------------------------------------------
# healthprobe: tiny HTTP probe for HEALTHCHECK. The chiseled image has no shell, curl or wget,
# so the check runs on the .NET runtime that is already there. Architecture-neutral IL.
# ---------------------------------------------------------------------------------------------
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-noble AS healthprobe
WORKDIR /src
COPY Directory.Build.props nuget.config ./
COPY docker/healthcheck/HealthProbe.cs docker/healthcheck/
RUN dotnet publish docker/healthcheck/HealthProbe.cs --configuration Release --output /app/healthcheck -p:UseAppHost=false

# ---------------------------------------------------------------------------------------------
# migrator: one-shot container that applies migrations and exits.
# Same base as the API, so both images share their runtime layers.
# ---------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled AS migrator
WORKDIR /app
COPY --from=bundle /app/efbundle ./efbundle
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---------------------------------------------------------------------------------------------
# api (default): Ubuntu chiseled ASP.NET Core runtime. Distroless, non-root (APP_UID), port 8080.
# ---------------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-noble-chiseled AS api
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
WORKDIR /app
COPY --from=healthprobe /app/healthcheck /healthcheck
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
    CMD ["dotnet", "/healthcheck/HealthProbe.dll", "http://localhost:8080/health/ready"]
ENTRYPOINT ["dotnet", "BookingSystem.Api.dll"]
