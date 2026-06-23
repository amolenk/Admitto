## 1. Logging Configuration

- [x] 1.1 Add explicit production log-level baselines for API and Worker, including `Amolenk.Admitto`, `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore.Database.Command`, `Azure.Messaging.ServiceBus`, `Azure.Core`, `Quartz`, and `Microsoft.Hosting.Lifetime`.
- [x] 1.2 Preserve local development diagnostics while suppressing recurring Service Bus empty-receive and EF Core command noise unless a developer deliberately raises those categories.
- [x] 1.3 Review existing Admitto `LogError` usage in API and Worker paths touched by observability alerts and confirm expected business outcomes are not logged as operator-actionable errors.

## 2. Sampling Configuration

- [x] 2.1 Add an observability options section for Azure Monitor sampling ratio with validation and a documented default.
- [x] 2.2 Update `Admitto.ServiceDefaults` to read the sampling ratio from configuration when Application Insights export is enabled.
- [x] 2.3 Wire production/container environment configuration for the sampling ratio where AppHost or Bicep needs to provide it.

## 3. AppHost Azure Alerting Provisioning

- [x] 3.1 Add AppHost publish parameters for operator alert notification targets, such as email and/or webhook values.
- [x] 3.2 Add an Azure Monitor action group to the AppHost deployment graph, preferably via `ConfigureInfrastructure(...)` and `Azure.Provisioning.Monitor`.
- [x] 3.3 Add a scheduled query alert for exception telemetry from API and Worker over the Application Insights / Log Analytics workspace through AppHost-managed Azure provisioning.
- [x] 3.4 Add a scheduled query alert for API and Worker `Error`/`Critical` logs over the Application Insights / Log Analytics workspace through AppHost-managed Azure provisioning.
- [x] 3.5 Decide and encode the initial failed-request alert threshold, or explicitly defer failed-request alerting if it would create too much noise from expected 4xx responses.
- [x] 3.6 If `Azure.Provisioning.Monitor` cannot express the alert resources cleanly, add a custom Bicep file under `src/Admitto.AppHost/` and reference it from AppHost with `AddAzureBicepResource(...)` rather than relying on standalone `infra/` deployment.

## 4. Documentation

- [x] 4.1 Update `docs/arc42/08-crosscutting-concepts.md` with the production observability policy, log severity expectations, and sampling behavior.
- [x] 4.2 Update `docs/arc42/07-deployment-view.md` with the Aspire-managed Azure Application Insights / Log Analytics alerting shape and operator tuning knobs.

## 5. Verification

- [x] 5.1 Run the architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 5.2 Start Aspire with production-equivalent logging overrides and verify idle Worker logs no longer include empty Service Bus receive logs or EF Core SQL command chatter.
- [x] 5.3 Verify the configured sampling ratio is reflected in the API and Worker runtime configuration when Application Insights export is enabled.
- [x] 5.4 Validate the AppHost-generated Azure deployment artifacts include the action group and scheduled query alert rules.
- [ ] 5.5 In an Azure or deployment-equivalent environment, force a controlled API/Worker exception and confirm the expected alert rule evaluates and targets the action group.
