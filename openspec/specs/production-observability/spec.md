## Purpose

Define the production observability baseline for Admitto services, including log noise suppression, telemetry sampling, alerting signals, and operator/developer documentation expectations.

## Requirements

### Requirement: Production log levels suppress routine infrastructure noise
Production API and Worker hosts SHALL configure logging so routine framework and SDK activity does not emit informational logs for idle operation. The configuration SHALL suppress informational logs from EF Core SQL command execution, Azure Service Bus receive/link internals, Azure SDK internals, Quartz scheduler internals, and ASP.NET Core framework categories while retaining warnings and errors from those categories.

#### Scenario: Idle worker does not emit Service Bus poll logs
- **WHEN** the Worker is running in a production-equivalent logging configuration and the queue is idle
- **THEN** empty Service Bus receive attempts do not emit `Information` log records

#### Scenario: Idle database polling does not emit SQL command logs
- **WHEN** the Worker performs routine outbox or scheduled-job database polling in a production-equivalent logging configuration
- **THEN** EF Core SQL command execution does not emit `Information` log records

#### Scenario: Framework warnings and errors are retained
- **WHEN** EF Core, Azure Service Bus, Azure SDK, Quartz, or ASP.NET Core emits a warning or error
- **THEN** the warning or error remains eligible for telemetry export

### Requirement: Admitto application logs remain operationally visible
Production API and Worker hosts SHALL keep Admitto application logs at `Information` by default while preserving all `Warning`, `Error`, and `Critical` records. Expected business validation outcomes SHALL NOT be logged as errors solely to make them visible in telemetry.

#### Scenario: Application lifecycle log is exported
- **WHEN** API or Worker emits an Admitto application `Information` log in production
- **THEN** the log remains eligible for telemetry export

#### Scenario: Unexpected application failure is exported as error
- **WHEN** API or Worker logs an unexpected application failure at `Error` or `Critical`
- **THEN** the log remains eligible for telemetry export and alert evaluation

### Requirement: Azure Monitor sampling is configurable
The Azure Monitor OpenTelemetry exporter SHALL read its sampling ratio from configuration when `APPLICATIONINSIGHTS_CONNECTION_STRING` is present. The system SHALL provide a safe default sampling ratio for production if no explicit value is configured, and local Aspire OTLP telemetry SHALL remain suitable for development diagnostics.

#### Scenario: Configured sampling ratio is applied
- **WHEN** a production deployment supplies an observability sampling ratio configuration value
- **THEN** the Azure Monitor exporter uses that ratio for sampled telemetry

#### Scenario: Default sampling ratio is applied
- **WHEN** Application Insights is configured and no sampling ratio configuration value is supplied
- **THEN** the Azure Monitor exporter uses the documented default sampling ratio

### Requirement: Exceptions and high-severity logs trigger Azure alerts
Production infrastructure SHALL provision Azure Monitor alert rules over the Application Insights / Log Analytics workspace for API and Worker exceptions and high-severity application logs. The alert rules SHALL be wired to an operator notification action group. The alert resources SHALL be part of the Aspire AppHost deployment graph so `aspire deploy` provisions them with the rest of the environment.

#### Scenario: Unhandled API exception triggers alert evaluation
- **WHEN** the API emits exception telemetry for an unhandled exception
- **THEN** an Azure Monitor alert rule evaluates the exception signal and notifies the configured action group when its threshold is met

#### Scenario: Worker processing failure triggers alert evaluation
- **WHEN** the Worker emits an `Error` or `Critical` log for queue processing, job execution, or startup reconciliation failure
- **THEN** an Azure Monitor alert rule evaluates the high-severity log signal and notifies the configured action group when its threshold is met

#### Scenario: Aspire deploy includes alert resources
- **WHEN** the production environment is deployed through Aspire
- **THEN** the action group and scheduled query alert rules are included in the AppHost-generated Azure deployment

### Requirement: Observability policy is documented
The architecture documentation SHALL describe the production observability baseline, including log categories suppressed as noise, application log expectations, sampling configuration, and alerting signals.

#### Scenario: Operator can find telemetry tuning guidance
- **WHEN** an operator needs to adjust log noise or sampling in production
- **THEN** the arc42 documentation identifies the relevant configuration knobs and the expected trade-offs

#### Scenario: Developer can classify new logs correctly
- **WHEN** a developer adds new background work or exception handling
- **THEN** the arc42 documentation explains which events should be logged as `Information`, `Warning`, or `Error` for alerting purposes
