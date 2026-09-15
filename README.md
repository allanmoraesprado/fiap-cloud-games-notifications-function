# FIAP Cloud Games — Notifications Function

Serverless notifications for the FIAP Cloud Games platform (**Phase 3**). An **Azure Functions**
project (.NET 8, **isolated worker**) with two **Kafka-triggered** functions that replace the
always-running `fiap-cloud-games-notifications-api` container from Phase 2 in the main flow.
It simulates e-mails by **logging them** (no SMTP, no real provider — intentional for this
academic MVP).

Part of the six-repository solution (`users-api`, `catalog-api`, `payments-api`,
`notifications-api` [Phase 2 history, tag `phase-2`], **`notifications-function`**, `orchestration`).
For the full system runbook see **`fiap-cloud-games-orchestration`**.

---

## Functions

| Function | Trigger topic | Consumer group | Action |
|---|---|---|---|
| `UserCreatedNotification` | `fcg.users.created` | `notifications-function` | Log a simulated **welcome e-mail** (`[WELCOME EMAIL]`) |
| `PaymentProcessedNotification` | `fcg.payments.processed` | `notifications-function` | Log a **purchase confirmation** (`[PURCHASE CONFIRMATION]`) only when `Status` is `Approved`; `Rejected` is logged and produces no e-mail |

Both functions share the consumer group `notifications-function`, distinct from CatalogAPI's
`catalog-service`, so `fcg.payments.processed` **fans out** to both. Event contracts:
`fiap-cloud-games-orchestration/contracts/README.md` (mirrored in `Contracts/`).

### Behaviour (validated in the P3-M0 spike)

- The Kafka extension delivers a JSON **envelope** (`Offset`, `Partition`, `Topic`, `Timestamp`,
  `Value`, `Key`, `Headers`); the event JSON is the string in `Value`. Parsing is two-step
  (`Messaging/KafkaEventParser`).
- Malformed JSON (envelope or event) is logged as a **warning** and the message is skipped; the
  host keeps running. There is **no retry policy**; the extension commits the offset after each
  execution (`CommitOnFailure`), the same at-least-once / skip-poison semantics as Phase 2.
- A brand-new consumer group starts from the **earliest** offset (extension default).
- No storage account is required for the Kafka trigger; `AzureWebJobsStorage` is a local placeholder.

---

## Tech

.NET 8 · Azure Functions v4 isolated worker (`Microsoft.Azure.Functions.Worker` 2.52.0,
`Worker.Sdk` 2.0.7) · `Microsoft.Azure.Functions.Worker.Extensions.Kafka` 4.3.3 ·
xUnit/FluentAssertions. Layout: `src/NotificationsFunction` (`Functions`, `Contracts`,
`Messaging`) + `tests/NotificationsFunction.Tests`.

---

## Configuration

Settings use the same `Kafka__*` names as the other FCG services (environment variables in
Compose/Kubernetes, `Values` in `local.settings.json` locally). Inside the trigger attributes they
are referenced as `%Kafka:Name%` (`%Kafka__Name%` does not resolve).

| Setting | Meaning | Local default |
|---|---|---|
| `Kafka__BootstrapServers` | Kafka bootstrap (host dev / `kafka:9092` in containers) | `localhost:29092` |
| `Kafka__UserCreatedTopic` | Topic for `UserCreatedEvent` | `fcg.users.created` |
| `Kafka__PaymentProcessedTopic` | Topic for `PaymentProcessedEvent` | `fcg.payments.processed` |
| `Kafka__ConsumerGroup` | Consumer group id | `notifications-function` |
| `FUNCTIONS_WORKER_RUNTIME` | Functions worker | `dotnet-isolated` |
| `AzureWebJobsStorage` | Not used by the Kafka trigger; local placeholder | `UseDevelopmentStorage=true` |

`local.settings.json` is **git-ignored**. Copy the committed example:

```bash
cp src/NotificationsFunction/local.settings.example.json src/NotificationsFunction/local.settings.json
```

No secrets are used locally (Kafka is PLAINTEXT). **No real secrets are committed.**

---

## Run locally (main path: Azure Functions Core Tools)

Prerequisites: .NET SDK 8+ (SDK 10 also builds `net8.0`), Docker Desktop,
[Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
(`npm install -g azure-functions-core-tools@4`).

1. Start Kafka from the orchestration repo (at least `kafka` + `kafka-init`; the full stack also works):
   ```bash
   cd ../fiap-cloud-games-orchestration && docker compose up -d kafka kafka-init
   ```
2. Create `local.settings.json` from the example (see above).
3. Run the function host **from the project folder** (Core Tools writes a default `host.json`
   into whatever folder it is run from):
   ```bash
   cd src/NotificationsFunction
   func start
   ```
4. Trigger events: register a user in UsersAPI (`POST /api/auth/register`) → `[WELCOME EMAIL]`;
   complete a purchase (CatalogAPI `POST /api/library/acquire/{gameId}` → PaymentsAPI) →
   `[PURCHASE CONFIRMATION]` for approved payments only.

Consumer-group evidence:
```bash
docker compose exec kafka /opt/kafka/bin/kafka-consumer-groups.sh --bootstrap-server localhost:9092 --describe --group notifications-function
```
(On Windows run `docker compose exec` from PowerShell; Git Bash rewrites the `/opt/...` path.)

## Test

```bash
dotnet test
```

## Docker (container path, for Compose/Kubernetes validation)

```bash
docker build -t fcg-notifications-function .
docker run --rm --network fcg-orchestration_default \
  -e AzureWebJobsStorage="UseDevelopmentStorage=true" \
  -e Kafka__BootstrapServers="kafka:9092" \
  -e Kafka__UserCreatedTopic="fcg.users.created" \
  -e Kafka__PaymentProcessedTopic="fcg.payments.processed" \
  -e Kafka__ConsumerGroup="notifications-function" \
  fcg-notifications-function
```

The image is built from the official `mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0`
base. Compose wiring and Kubernetes manifests are added in later Phase 3 milestones.

---

## Future cloud deployment (documented only, not implemented)

The project is a real Azure Functions app and could be published to an **Azure Function App**;
this academic delivery stays **local-first**. What a real deployment would need:

| Item | Placeholder / note |
|---|---|
| Hosting plan | Premium or Dedicated (the Kafka trigger is not supported on the classic Consumption plan) |
| Broker reachable from Azure | Event Hubs (Kafka protocol) or a managed Kafka with `SaslSsl`; the trigger attributes would switch `Protocol`/`AuthenticationMode` and read `Kafka__Username` / `Kafka__Password` app settings |
| Secrets | Azure Key Vault references instead of app-setting placeholders |
| Telemetry | Application Insights via `APPLICATIONINSIGHTS_CONNECTION_STRING` (the OpenTelemetry/Azure Monitor packages from the default template were intentionally removed to keep the project lean) |
| Scale-to-zero on Kubernetes | KEDA Kafka scaler |

None of the above is configured; no cloud resources or real secrets exist in this repository.
