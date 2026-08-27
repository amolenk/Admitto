# ADR-015: Azure Service Bus with Push-Based Consumption

## Status

Accepted. Supersedes [ADR-004](adr-004-azure-storage-queues.md).

## Context

Admitto replaced Azure Storage Queues with Azure Service Bus as the outbox dispatch target, but that technology change was never recorded as an ADR, so ADR-004 remained the only written decision on queue technology.

The Worker's consumer was also not the SDK's standard one.
It ran a hand-rolled receive loop over `ServiceBusReceiver.ReceiveMessageAsync` with a 5-second wait time, a globally lowered `RetryOptions.TryTimeout` of 5 seconds, and a rule that disposed and recreated the receiver after six consecutive empty polls.
That loop existed to work around the Azure Service Bus emulator: at the time it was written, the emulator only checked its MSSQL backend for new messages when it received a fresh AMQP credit, so a passive listener could miss a message for up to the 60-second default `TryTimeout`, and the link could stall silently.

The workaround had a standing cost.
Re-polling every 5 seconds and rebuilding the AMQP link every 30 seconds made the Azure SDK narrate roughly 29 `Information` records per idle minute, plus an application `Debug` record per link rebuild, so an idle Worker produced continuous log noise that carried no signal.
It also forfeited the SDK behaviour the loop did not reimplement: automatic link recovery and message lock renewal for handlers that outlive the queue's lock duration.

Measured against the emulator the project runs today (`mcr.microsoft.com/azure-messaging/servicebus-emulator:2.0.0`), the stall does not reproduce.
A `ServiceBusProcessor` on default settings delivered in 25-336 ms, including after 90 seconds of idle, and emitted roughly 1 `Information` record per idle minute against the loop's 29.

## Decision

- Use **Azure Service Bus** as the outbox message dispatch target, kept behind `IOutboxMessageSender` so the implementation stays replaceable.
- Consume with the SDK's push-based `ServiceBusProcessor` rather than a hand-rolled receive loop with a lowered `TryTimeout`.
- Bound `ServiceBusRetryOptions.MaxDelay` to 5 seconds. The receive loop recovered from a fault in about a second by construction; the SDK's 60-second default would let a consumer idle far longer after a blip, so recovery latency is set deliberately rather than inherited.
- Settle messages explicitly (`AutoCompleteMessages = false`) and keep dispatch sequential (`MaxConcurrentCalls = 1`), preserving the delivery semantics the receive loop had.
- Suppress the `Azure.Messaging.ServiceBus` and `Azure.Core` log categories in the shared `appsettings.json` baseline rather than only in production, because they narrate SDK mechanics that carry no application signal in any environment.

## Rationale

- The emulator defect that justified the custom loop is fixed in the emulator version the project runs, so the workaround now costs noise and capability without buying reliability.
- Delivery latency does not regress: both approaches deliver in tens of milliseconds, because a message arriving at an established link is pushed immediately.
- The processor adds automatic link recovery and message lock renewal, which the loop did not implement and which matter for slow handlers such as bulk-email fan-out.
- Removing the global `TryTimeout` override stops a consumer-driven workaround from also shortening send timeouts in the API host. The remaining `MaxDelay` override is narrower: it targets recovery latency, which the consumer genuinely cares about, rather than poll frequency.

## Consequences

- Idle Worker Service Bus logging drops to nothing; a live run over three idle minutes emitted zero Service Bus records.
- Transient link and connection faults are no longer logged per occurrence by application code. They surface through the processor's error handler as warnings, and a genuine outage shows up as the warning repeating rather than as a single line.
- The project now depends on emulator behaviour rather than working around it. If a future emulator version reintroduces the stall, the symptom is delayed local delivery, and the fix is to pin or upgrade the emulator image, not to reintroduce a polling loop.
- Raising throughput later is a matter of raising `MaxConcurrentCalls`, but that requires confirming handlers tolerate concurrent delivery on a single replica.
