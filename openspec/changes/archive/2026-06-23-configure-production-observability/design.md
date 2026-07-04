## Context

`Admitto.ServiceDefaults` configures OpenTelemetry tracing, metrics, logs, health checks, and Azure Monitor export when `APPLICATIONINSIGHTS_CONNECTION_STRING` is present. AppHost publish mode creates Application Insights backed by Log Analytics, and API/Worker Container Apps receive the Application Insights connection string.

Local Aspire log inspection shows three categories that create most idle noise:

- `Azure.Messaging.ServiceBus` logs every empty `ReceiveMessageAsync` poll at `Information`.
- `Microsoft.EntityFrameworkCore.Database.Command` logs every SQL command at `Information`, including idle outbox/job queries.
- Quartz emits informational scheduler/runtime chatter during Worker startup.

The Worker currently uses an explicit Service Bus receive loop to keep local emulator delivery latency bounded and to refresh stalled AMQP links. This change treats that as a queue reliability decision and solves the log-cost problem through observability policy, not by changing queue semantics.

## Goals / Non-Goals

**Goals:**

- Reduce production telemetry ingestion noise from idle Worker polling, EF Core command logging, Azure SDK internals, and Quartz chatter.
- Preserve useful Admitto application `Information` logs and all warnings/errors.
- Make Azure Monitor sampling ratio configurable per environment.
- Add Azure Monitor alerts for exceptions and high-severity application logs emitted by API and Worker.
- Document the production observability baseline and tuning knobs.

**Non-Goals:**

- Replacing the explicit Service Bus receive loop with `ServiceBusProcessor`.
- Introducing Serilog or another logging framework.
- Adding custom dashboards beyond alert rules.
- Changing functional queue dispatch, outbox retry, Quartz job, or API behavior.
- Implementing adaptive or tail-based sampling.

## Decisions

### D1: Suppress noisy categories with log-level configuration

**Decision**: Configure production log levels so framework and SDK internals default to warnings while Admitto application categories remain at information.

Recommended baseline:

| Category | Level | Rationale |
| :------- | :---- | :-------- |
| `Default` | `Information` | Keeps general operational signal. |
| `Amolenk.Admitto` | `Information` | Keeps domain/application lifecycle logs. |
| `Microsoft.AspNetCore` | `Warning` | Suppresses routine framework request noise. |
| `Microsoft.EntityFrameworkCore.Database.Command` | `Warning` | Suppresses every SQL command while preserving command failures. |
| `Azure.Messaging.ServiceBus` | `Warning` | Suppresses empty receive poll logs and link lifecycle noise. |
| `Azure.Core` | `Warning` | Suppresses Azure SDK internals except warnings/errors. |
| `Quartz` | `Warning` | Suppresses scheduler chatter while preserving job/scheduler failures. |
| `Microsoft.Hosting.Lifetime` | `Information` | Keeps container startup/shutdown signal. |

**Rationale**: Log levels are the cheapest and most deterministic way to reduce log ingestion. Sampling traces does not reliably remove log records, and filtering after ingestion still incurs cost.

**Alternative considered**: Keep categories at information and use sampling. This leaves logs as the dominant cost source and risks missing exceptions if log sampling is applied incorrectly.

### D2: Keep trace sampling fixed-ratio and configurable

**Decision**: Continue using Azure Monitor's fixed-ratio sampling, but read the ratio from configuration with a safe default.

**Rationale**: Current code hard-codes `SamplingRatio = 0.1f`. A configuration value lets production tune cost without rebuilds and lets incident response temporarily increase fidelity.

**Default**: `0.1` for production, `1.0`/always-on visibility locally through Aspire's OTLP exporter.

**Alternative considered**: Disable sampling until volume becomes a problem. This delays cost feedback and makes the first production usage spike more expensive than needed.

### D3: Do not sample away error alerts

**Decision**: Alerts are based on error logs and exception telemetry that remain unsuppressed by category-level filtering. Normal successful traces/dependencies can be sampled.

**Rationale**: Sampling is acceptable for volume estimation and distributed trace exploration, but operators want deterministic notification for unhandled exceptions and Worker processing failures.

**Alternative considered**: Alert only on failed sampled traces. This can miss low-volume failures under fixed-ratio sampling.

### D4: Provision Azure Monitor alert rules from AppHost

**Decision**: Model Azure Monitor scheduled query rules and their action group in `Admitto.AppHost` so `aspire deploy` provisions them with the rest of the Azure environment. Prefer Aspire's `ConfigureInfrastructure(...)` customization with `Azure.Provisioning.Monitor` resources (`ActionGroup`, `ScheduledQueryRule`, and related action/condition types). Use a custom Bicep resource referenced from AppHost only if the typed provisioning surface cannot express the required scheduled query rule shape.

**Initial alert rules:**

- Any exception telemetry for API/Worker over a short evaluation window.
- Any error or critical application log for API/Worker over a short evaluation window.
- Optional failed HTTP request threshold for API, with a threshold above one-off client errors.

**Rationale**: Alerting should be part of the same Aspire deployment graph as Application Insights, Log Analytics, API, and Worker. A standalone `infra/` Bicep module is easy to forget when deployments run through `aspire deploy`; AppHost-owned provisioning keeps the alert resources coupled to the actual deployment path. Scheduled query rules can query workspace-backed Application Insights tables such as `AppExceptions`, `AppTraces`, and `AppRequests`.

**Alternatives considered**:

- Standalone Bicep under `infra/`: valid only if a separate deployment pipeline always runs it; not reliable for Aspire-first deployment.
- Application Insights smart detection only: useful but opaque, delayed, and not a substitute for explicit operational contracts.

### D5: Document observability as a cross-cutting production policy

**Decision**: Update arc42 deployment and cross-cutting documentation with the log-level baseline, sampling knob, and alerting model.

**Rationale**: Observability spans API, Worker, AppHost, and infrastructure. Future features and jobs need to know what log levels mean and which categories are expected to page operators.

## Risks / Trade-offs

- **Risk: Suppressing EF Core command logs hides query details during production incidents.** → Mitigation: warning/error command failures remain logged; operators can temporarily raise the category level through configuration during an investigation.
- **Risk: Fixed-ratio sampling may make successful request traces incomplete.** → Mitigation: keep request/log correlation available for sampled traces and keep alerting on unsuppressed errors rather than sampled success telemetry.
- **Risk: Kusto table names differ by Application Insights workspace mode or provider version.** → Mitigation: verify queries against the deployed workspace and adjust AppHost-provisioned scheduled query definitions before enabling severity/action groups in production.
- **Risk: `Azure.Provisioning.Monitor` lacks a needed property or has an awkward preview API.** → Mitigation: keep the alert contract in AppHost, but switch the implementation detail to `AddAzureBicepResource(...)` with a custom Bicep file referenced from AppHost.
- **Risk: Alerts page on expected business-rule failures if logged as errors.** → Mitigation: keep expected validation/business outcomes below `Error`; reserve `Error` for unexpected failures and operator-actionable data issues.
- **Risk: Alert fatigue from a `> 0` error-log threshold.** → Mitigation: start with short evaluation windows and review first-week alerts; raise thresholds or narrow categories if specific known-benign errors emerge.

## Migration Plan

1. Add explicit production log-level settings for API and Worker.
2. Make Azure Monitor sampling ratio configurable in service defaults.
3. Add AppHost publish/container environment wiring for the sampling configuration if needed.
4. Add AppHost Azure provisioning for an action group and scheduled query alert rules, preferably through `ConfigureInfrastructure(...)` and `Azure.Provisioning.Monitor`.
5. If the typed provisioning API cannot express the alert rules cleanly, add a custom Bicep file under the AppHost project and reference it with `AddAzureBicepResource(...)` so `aspire deploy` still deploys the alerts.
6. Update arc42 observability/deployment documentation.
7. Verify locally with Aspire that idle Worker logs no longer include empty Service Bus receives or EF command chatter when production-equivalent levels are applied.
8. Verify in Azure that a forced API/Worker exception appears in telemetry and triggers the expected alert.

**Rollback**: Revert configuration and Bicep changes. Runtime behavior is unchanged; rollback affects only telemetry volume and alert resources.

## Open Questions

- Which notification target should the action group use initially: email, Teams/webhook, or both?
- Should failed HTTP requests alert immediately, or only after a threshold that filters out expected 4xx client mistakes?
