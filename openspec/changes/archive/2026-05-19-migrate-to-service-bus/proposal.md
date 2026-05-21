## Why

Azure Storage Queues use a polling model with up to 5 seconds latency between message publication and processing. Azure Service Bus provides push-based delivery via AMQP long-poll, giving near-zero latency at comparable cost (Basic tier is ~$0.05/million operations). The private VNET requirement that previously forced the project back to Storage Queues (Premium tier is needed for private endpoints) has been dropped — the simpler deployment model now makes Service Bus the better choice.

## What Changes

- Replace `AzureStorageQueueProcessor` polling loop with `ServiceBusProcessor` (push-based delivery)
- Replace `OutboxMessageSender` queue client (`QueueClient`) with Service Bus sender (`ServiceBusSender`)
- Replace Aspire `AddAzureStorageQueues` / `Azurite` emulator with `AddAzureServiceBus` / Service Bus emulator (with MSSQL Server sidecar workaround for ARM/newer-kernel compatibility)
- Swap NuGet packages: `Aspire.Azure.Storage.Queues` + `Azure.Storage.Queues` → `Aspire.Azure.Messaging.ServiceBus` + `Azure.Messaging.ServiceBus`
- Swap AppHost packages: `Aspire.Hosting.Azure.Storage` → `Aspire.Hosting.Azure.ServiceBus`
- Remove the AppHost `AzureStorage` extension helpers (queue creation hook for Azurite — not needed, queues are declared in the Aspire model)
- Simplify `infra/modules/serviceBus.bicep` to Basic SKU without private endpoints; wire it into `main.bicep` in place of the storage queue resources

## Capabilities

### New Capabilities

_None — this is a transport infrastructure migration with no new functional capabilities._

### Modified Capabilities

_None — the `queue-message-dispatch` spec requirements are unchanged. The dispatch logic (`QueueMessageDispatcher`, handler resolution, UoW commit) is transport-agnostic and is unaffected._

## Impact

- **AppHost / local dev**: Aspire will spin up the Service Bus emulator container (with MSSQL Server sidecar) instead of Azurite; persistent lifetime retained
- **Worker**: `MessageQueueProcessor` transitions from a polling BackgroundService to a push-driven one; no change to handler or dispatcher code
- **API**: `OutboxMessageSender` sends to a `ServiceBusSender` instead of `QueueClient`; CloudEvent payload encoded as `BinaryData` (binary protocol mode)
- **Infra (Bicep)**: Service Bus namespace (Basic SKU) replaces queue storage resources in `main.bicep`; storage account remains for Data Protection blob key storage
- **Connection name**: changes from `"queues"` to `"messaging"` across AppHost and DI registration
