## Why

Production observability is currently useful but too noisy for cost-conscious Azure operation: idle Worker runs emit repeated Service Bus receive logs and EF Core SQL command logs at `Information`, while Azure Monitor sampling is hard-coded and alerting for exceptions is not declared in infrastructure. We need a clear, low-noise observability baseline that preserves actionable failures and operational diagnostics without ingesting routine polling chatter.

## What Changes

- Add a production observability policy covering log-level hygiene, telemetry sampling, and exception alerting.
- Suppress noisy framework/SDK categories in production, including idle Service Bus receive logs, EF Core command logs, and Quartz startup/runtime chatter.
- Keep Admitto application logs at `Information` by default, with warnings/errors always retained.
- Make Azure Monitor sampling configurable instead of hard-coded, while preserving local Aspire visibility.
- Provision Azure Monitor alerting for exceptions and high-severity application logs from API and Worker telemetry through the Aspire AppHost deployment graph.
- Document the observability conventions in arc42 deployment/cross-cutting documentation.

## Capabilities

### New Capabilities

- `production-observability`: Production telemetry, sampling, log noise control, and exception alerting for API and Worker hosts.

### Modified Capabilities

_None — this change does not alter functional queue dispatch, email, registration, or admin UI behavior._

## Impact

- **Service defaults**: Azure Monitor exporter configuration becomes environment-configurable.
- **API / Worker configuration**: Production log levels are made explicit for Admitto, ASP.NET Core, EF Core, Azure SDK, Service Bus, Quartz, and hosting lifetime categories.
- **AppHost / Azure provisioning**: Application Insights / Log Analytics gains alert resources and an action-group integration point through Aspire-managed Azure provisioning; custom Bicep may be referenced from AppHost only if the typed provisioning API is insufficient.
- **Operations**: Exception and error-log alerts notify operators without relying on manual dashboard inspection.
- **Documentation**: `docs/arc42/07-deployment-view.md` and `docs/arc42/08-crosscutting-concepts.md` describe the observability baseline and tuning knobs.
