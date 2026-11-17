# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first for better layer caching
COPY src/NotificationsFunction/NotificationsFunction.csproj src/NotificationsFunction/
RUN dotnet restore src/NotificationsFunction/NotificationsFunction.csproj

# Copy sources and publish into the Functions host script root
COPY src/ src/
RUN dotnet publish src/NotificationsFunction/NotificationsFunction.csproj -c Release -o /home/site/wwwroot /p:UseAppHost=false

# ---------- latest .NET 8 servicing runtime (used to patch the Functions image below) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS dotnet8

# ---------- runtime stage: official Azure Functions host image (isolated worker, .NET 8) ----------
# Used for the Docker Compose service and Kubernetes; the main development path is
# `func start` on the host. Kafka__* settings are injected at runtime.
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final

# The published Functions image lags behind Debian and .NET security updates (the CI image scan
# fails on fixable CRITICAL findings). Patch it: upgrade the OS packages, and replace the .NET 8
# shared runtime used by the isolated worker with the latest servicing release, keeping only the
# newest 8.0.x patch (the worker rolls forward to the latest installed patch by default).
RUN apt-get update \
    && apt-get upgrade -y --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*
COPY --from=dotnet8 /usr/share/dotnet/shared/Microsoft.NETCore.App/ /usr/share/dotnet/shared/Microsoft.NETCore.App/
COPY --from=dotnet8 /usr/share/dotnet/shared/Microsoft.AspNetCore.App/ /usr/share/dotnet/shared/Microsoft.AspNetCore.App/
RUN for fw in /usr/share/dotnet/shared/Microsoft.NETCore.App /usr/share/dotnet/shared/Microsoft.AspNetCore.App; do \
        ls -d "$fw"/8.0.* | sort -V | head -n -1 | xargs -r rm -rf; \
    done \
    && ls /usr/share/dotnet/shared/Microsoft.NETCore.App /usr/share/dotnet/shared/Microsoft.AspNetCore.App

# Commit SHA of the build (set by CI: --build-arg APP_VERSION=<sha>); exposed to the app and as
# OCI metadata so a running pod can be traced back to its commit.
ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION
LABEL org.opencontainers.image.revision=$APP_VERSION \
      org.opencontainers.image.source="https://github.com/allanmoraesprado/fiap-cloud-games-notifications-function"
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
COPY --from=build /home/site/wwwroot /home/site/wwwroot
