# FIAP Cloud Games — Notifications Function

Serverless notifications for the FIAP Cloud Games platform (**Phase 3**). An **Azure Functions**
project (.NET 8, isolated worker) triggered by **Kafka messages** that replaces the always-running
`fiap-cloud-games-notifications-api` container from Phase 2 in the main flow.

> **Status: P3-M0 skeleton.** This repository holds only this README and `.gitignore`.
> The Functions project, tests, Dockerfile, `k8s/` manifests and documentation are added in **P3-M1**.
> The Phase 2 service is kept as history in `fiap-cloud-games-notifications-api` (tag `phase-2`).

Part of the six-repository solution (`users-api`, `catalog-api`, `payments-api`,
`notifications-api` [Phase 2 history], `notifications-function`, `orchestration`).

---

## Planned responsibilities (P3-M1)

| Function | Topic | Consumer group | Action |
|---|---|---|---|
| `UserCreatedNotification` | `fcg.users.created` | `notifications-function` | Log a simulated welcome e-mail |
| `PaymentProcessedNotification` | `fcg.payments.processed` | `notifications-function` | Log a purchase confirmation (**Approved** only) |

No SMTP and no real e-mail provider (intentional for the academic delivery). Event contracts:
`fiap-cloud-games-orchestration/contracts/README.md`.

## Runtime model

- **Main development path:** `func start` (Azure Functions Core Tools v4) on the host, consuming the
  Compose Kafka through `localhost:29092`.
- **Container path:** image built from the official `mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0`
  base, for Docker Compose (profile) and local Kubernetes validation.
- **Cloud deployment** (Azure Function App on a Premium/Dedicated plan, Kafka over SASL_SSL, Key Vault,
  Application Insights) is documented only as a future improvement — not implemented.

## Configuration (placeholders only)

Environment variables follow the same `Kafka__*` convention as the other services
(`Kafka__BootstrapServers`, `Kafka__UserCreatedTopic`, `Kafka__PaymentProcessedTopic`, `Kafka__ConsumerGroup`).
Inside the trigger attributes they are referenced as `%Kafka:Name%`. `local.settings.json` is git-ignored;
a `local.settings.json.example` with local/dev placeholders will be committed in P3-M1. **No real secrets are committed.**

## Decisions validated by the P3-M0 spike (2026-09-14)

- Kafka trigger extension `Microsoft.Azure.Functions.Worker.Extensions.Kafka` 4.3.3 runs on Windows via Core Tools and in a Linux container.
- No storage account / Azurite required for the Kafka trigger locally.
- The worker receives a JSON **envelope** (`Offset`, `Partition`, `Topic`, `Timestamp`, `Value`, `Key`, `Headers`); the event JSON is in `Value`.
- Offsets are committed after each execution (also on failure, `CommitOnFailure=true`); a new consumer group starts from **earliest** by default.
