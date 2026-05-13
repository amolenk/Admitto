## Context

Admitto uses an EF Core `DomainEventsInterceptor` that fires during `SaveChangesAsync`. It
dispatches domain events synchronously to `IDomainEventHandler<T>` (in-transaction) and writes
outbox rows for deferred/cross-module delivery via `MessagePolicy`.

The current three-tier taxonomy is:

| Tier | Type | Transport | Primitives? |
|------|------|-----------|-------------|
| 1 | `IDomainEvent` | In-tx mediator dispatch | No — carries domain VOs |
| 2 | `IModuleEvent` | Outbox → queue → `IModuleEventHandler<T>` | Yes |
| 3 | `IIntegrationEvent` | Outbox → queue → `IIntegrationEventHandler<T>` | Yes |

Every `IModuleEventHandler<T>` in the codebase does exactly one thing: `mediator.SendAsync(command)`.
`ModuleEvent` therefore has no semantic weight — it is a serialization vehicle, not a domain concept.
`MessagePolicy` is the place that maps domain events to module/integration events, which means it
also owns the VO → primitive extraction — a responsibility that properly belongs at the handler level.

The proposed taxonomy collapses the middle tier:

| Tier | Type | Transport | Primitives? |
|------|------|-----------|-------------|
| 1 | `IDomainEvent` | In-tx mediator dispatch | No — never crosses the wire |
| 2 | `ICommand` | Direct mediator (in-tx) OR outbox → queue | Yes |
| 3 | `IIntegrationEvent` | Outbox → queue → `IIntegrationEventHandler<T>` | Yes |

`IDomainEventHandler<T>` becomes the single decision point: call the mediator directly
(same tx) or enqueue via `IOutbox` (deferred). The handler author makes the choice explicitly,
in code, where it is visible and testable.

## Goals / Non-Goals

**Goals:**

- Remove `IModuleEvent`, `ModuleEvent`, `IModuleEventHandler<T>`, `ModuleEventRouter`
- Remove `IMessagePolicy`, `MessagePolicy`, `OutboxWriter`, and all per-module policy classes
- Replace `IIntegrationEventOutbox` with a broader `IOutbox` that accepts both `ICommand` and `IIntegrationEvent`
- Make `DomainEventsInterceptor` a pure domain event dispatcher (no outbox coupling)
- Preserve identical observable behaviour for all existing async workflows
- Write an ADR capturing the final event taxonomy decisions

**Non-Goals:**

- Changing integration event contracts or the message bus topology
- Introducing dead-letter queues or poison-message handling (separate concern)
- Modifying how `IDomainEventHandler<T>` runs in-transaction (unchanged)
- Changing the Worker host's retry/polling infrastructure

## Decisions

### 1. `IOutbox` replaces `IIntegrationEventOutbox` (extend, don't wrap)

`IIntegrationEventOutbox` is already registered as a keyed scoped service per module, backed by
the correct `DbContext` instance for the current request scope. Adding a command overload to the
same interface and renaming it avoids a new abstraction layer and a new DI registration.

```csharp
// Application layer (Shared)
public interface IOutbox
{
    void Enqueue(ICommand command);
    void Enqueue(IIntegrationEvent integrationEvent);
}
```

The implementation (`Outbox`, formerly `IntegrationEventOutbox`) writes an `OutboxMessage` row
to the module's `DbSet<OutboxMessage>` for both overloads — same mechanics, different type string prefix.

**Alternative considered:** two separate interfaces (`ICommandOutbox`, `IIntegrationEventOutbox`).
Rejected: no meaningful benefit, forces handlers to inject two services instead of one, and the
boundary (commands vs events in the outbox) is already expressed by which overload is called.

---

### 2. No marker interface — `ICommand` types are registered by namespace convention

A marker interface (`IDeferredCommand`) would add a concept that developers need to learn and
apply correctly: "some commands are bus-routable, others aren't." This undermines the simplicity
goal of this change. The call site — `IOutbox.Enqueue(command)` — is already an explicit, deliberate
act. No additional opt-in marker is needed.

`MessageTypeRegistry` scans all `ICommand` implementations found in module assemblies by namespace
convention (`Amolenk.Admitto.Core.<Module>.Application.*`), same structural rule as integration
events. A handful of extra registered types that are never enqueued are harmless — they will never
be dispatched unless something explicitly calls `IOutbox.Enqueue` for them.

**Type string convention:** `command.{module-kebab}.{command-name-kebab}` (dropping the `-command`
suffix, consistent with how integration events drop `-integration-event`).
Example: `command.organization.register-external-user`

**Alternative considered:** marker interface `IDeferredCommand`. Rejected: adds semantic load
("what's the difference between a command and a deferred command?") with no practical safety
benefit, since enqueuing a command is already an explicit code-level decision.

---

### 3. `DomainEventsInterceptor` becomes a pure dispatcher

Remove all `IMessagePolicy` and `OutboxWriter` usage from the interceptor. After this change the
interceptor only calls `mediator.PublishDomainEventAsync(domainEvent)` per event. Outbox writes
happen inside handlers that inject `IOutbox`.

**Consequence:** outbox writes are now the handler author's responsibility. This is intentional —
it trades implicit policy enforcement for explicit, code-reviewable, individually testable behaviour.
Architecture tests will enforce that domain event handlers in modules that own outbox-capable
DbContexts are the only place `IOutbox` is injected (i.e., not from command handlers or jobs).

---

### 4. `QueueMessageDispatcher` adds a `Command` routing branch

```csharp
case MessageTypeRegistry.MessageKind.Command:
    var command = (ICommand)JsonSerializer.Deserialize(payload, entry.ClrType, JsonSerializerOptions.Web)!;
    await mediator.SendAsync(command, cancellationToken);
    break;
```

The existing `ModuleEventRouter` (which created a scope, called the handler, and committed the
UoW) is replaced by a direct mediator dispatch. UoW commit is the command handler's responsibility
via the standard endpoint / handler contract — same as any other command.

---

### 5. Each module event pair converts to a `IDomainEventHandler<T>`

| Before | After |
|--------|-------|
| `MessagePolicy.Configure<E>().PublishModuleEvent(...)` | `IDomainEventHandler<E>` → `IOutbox.Enqueue(command)` |
| `MessagePolicy.Configure<E>().PublishIntegrationEvent(...)` | `IDomainEventHandler<E>` → `IOutbox.Enqueue(integrationEvent)` |
| `IModuleEventHandler<M>` → `mediator.SendAsync(command)` | *(deleted)* |

Naming convention: `{DomainEventName}Handler` (same convention already used for domain event handlers).
If a domain event needs both in-tx work AND a deferred enqueue, use two separate handler classes
(one per concern) — the mediator dispatches to all registered handlers for the same event type.

---

### 6. `MessageTypeRegistry` drops `ModuleEvent` scanning, adds `Command` scanning

```csharp
if (typeof(ICommand).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
{
    var key = BuildCommandKey(type);    // "command.{module}.{name}"
    _byMessageType[key] = new Entry(type, MessageKind.Command, ModuleNameFor(type));
}
```

`MessageKind.ModuleEvent` is removed. Existing module event type strings in the queue
(`organization.user-created-module-event`) become unresolvable — see migration plan.

## Risks / Trade-offs

**[Risk] Pending module event outbox rows / queue messages at deploy time**
→ Drain the Azure Storage Queue and delete all pending `OutboxMessage` rows with a
`organization.*` / `email.*` type prefix before deploying. Acceptable because the application
is pre-production; affected async workflows (user provisioning, bulk email scheduling) will
self-heal on the next triggering action.

**[Risk] Handler author forgets to call `IOutbox.Enqueue`**
→ The outbox write is no longer guaranteed by infrastructure. Mitigated by: (a) the explicit
`IDeferredCommand` marker making intent clear, (b) domain-level integration tests that assert
outbox messages are written, (c) architecture tests that validate handler conventions.

**[Risk] A command placed outside the expected namespace convention silently fails to register in `MessageTypeRegistry`**
→ The `Outbox` implementation validates the namespace on `Enqueue` and throws at runtime (same
fail-fast pattern already in `IntegrationEventOutbox`). Cover with a unit test per module.

## Migration Plan

1. Drain the Azure Storage Queue (delete all pending messages).
2. Delete all `OutboxMessage` rows where `Type` matches `^organization\.|^email\.|^registrations\.` (module event prefix pattern).
3. Deploy the refactored code (single deployment — no phased rollout needed at this stage).
4. Smoke-test the two converted async paths: user creation → `RegisterExternalUser`, bulk email requested → `TriggerBulkEmailJob`.
5. Update `docs/arc42/08-crosscutting-concepts.md` §8.6 and add the ADR.

Rollback: revert the commit; replay any missed events via normal application flow (re-trigger the
action that raised the domain event). No data migration needed — the database schema is unchanged.

## Open Questions

*(none — all design decisions resolved during exploration)*
