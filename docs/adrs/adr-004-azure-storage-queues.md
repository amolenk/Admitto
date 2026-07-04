# ADR-004: Azure Storage Queues for Async Messaging

## Status

Accepted

## Context

Admitto uses a transactional outbox pattern to guarantee reliable delivery of module and integration events. The outbox persists event payloads in the same database transaction as domain changes. A dispatch mechanism then sends these messages to an external queue for asynchronous processing by the Worker host.

The team needed to choose a queue technology for this dispatch target.

## Decision

- Use **Azure Storage Queues** as the outbox message dispatch target.
- Abstract the queue behind `IOutboxMessageSender` so the implementation is replaceable.

## Rationale

- Azure Storage Queues are lightweight, inexpensive, and simple to operate — a good fit for the expected event volume of small free events.
- The `IOutboxMessageSender` abstraction decouples the outbox pattern from the specific queue technology. Switching to RabbitMQ, Azure Service Bus, or another transport requires only a new implementation of this interface.
- No need for advanced queue features (topic routing, dead-letter queues, ordering guarantees) at this stage.

## Consequences

### Positive

- Low operational overhead and cost.
- Clean abstraction boundary — queue technology is swappable without changing the outbox or domain logic.
- Azure Storage Queue emulator available for local development via Aspire AppHost.

### Negative

- Limited feature set compared to Service Bus or RabbitMQ (no topics, no built-in dead-lettering).
- If advanced routing or delivery guarantees are needed later, the transport must be replaced.
