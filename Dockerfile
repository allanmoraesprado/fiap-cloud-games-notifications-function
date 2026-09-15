# ---------- build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first for better layer caching
COPY src/NotificationsFunction/NotificationsFunction.csproj src/NotificationsFunction/
RUN dotnet restore src/NotificationsFunction/NotificationsFunction.csproj

# Copy sources and publish into the Functions host script root
COPY src/ src/
RUN dotnet publish src/NotificationsFunction/NotificationsFunction.csproj -c Release -o /home/site/wwwroot /p:UseAppHost=false

# ---------- runtime stage: official Azure Functions host image (isolated worker, .NET 8) ----------
# Used for the Docker Compose service and local Kubernetes; the main
# development path is `func start` on the host. Kafka__* settings are injected at runtime.
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
COPY --from=build /home/site/wwwroot /home/site/wwwroot
