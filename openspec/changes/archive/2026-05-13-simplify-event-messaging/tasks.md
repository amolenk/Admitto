## 1. Shared Infrastructure — `IOutbox`

- [x] 1.1 Rename `IIntegrationEventOutbox` → `IOutbox`; add `void Enqueue(ICommand command)` overload
- [x] 1.2 Rename `IntegrationEventOutbox` → `Outbox`; implement command serialization with `command.{module}.{name}` type string (strip `-command` suffix, same convention as integration events strip `-integration-event`)
- [x] 1.3 Update `AddModuleDatabaseServices` DI registration: replace `AddKeyedScoped<IIntegrationEventOutbox>` with `AddKeyedScoped<IOutbox>`

## 2. Shared Infrastructure — Interceptor & Dispatcher

- [x] 2.1 Simplify `DomainEventsInterceptor`: remove `IMessagePolicy` lookup, `OutboxWriter` construction, and `outboxWriter.TryEnqueue()` call — leave only `mediator.PublishDomainEventAsync()`
- [x] 2.2 Add `MessageKind.Command` to `MessageTypeRegistry`; replace `IModuleEvent` scanning with `ICommand` scanning using the `command.{module}.{name}` key builder
- [x] 2.3 Add `Command` routing branch to `QueueMessageDispatcher`: deserialize payload to `ICommand`, call `mediator.SendAsync(command, ct)`

## 3. Organization Module — Conversions

- [x] 3.1 Add `UserCreatedDomainEventHandler`: injects `[FromKeyedServices] IOutbox`, enqueues `RegisterExternalUserCommand` with deterministic command ID derived from the domain event ID
- [x] 3.2 Add `TicketedEventCreationRequestedDomainEventHandler`: injects `[FromKeyedServices] IOutbox`, enqueues `TicketedEventCreationRequestedIntegrationEvent`
- [x] 3.3 Delete `OrganizationMessagePolicy`, `UserCreatedModuleEvent`, `UserCreatedModuleEventHandler`
- [x] 3.4 Delete unused `TicketedEventCancelledModuleEvent` and `TicketedEventArchivedModuleEvent` from `Organization.Contracts`

## 4. Email Module — Conversions

- [x] 4.1 Add `BulkEmailJobRequestedDomainEventHandler`: injects `[FromKeyedServices] IOutbox`, enqueues `TriggerBulkEmailJobCommand`
- [x] 4.2 Delete `EmailMessagePolicy`, `BulkEmailJobRequestedModuleEvent`, `BulkEmailJobRequestedModuleEventHandler`

## 5. Registrations Module — Conversions

- [x] 5.1 Add `OtpCodeRequestedDomainEventHandler` → `IOutbox.Enqueue(OtpCodeRequestedIntegrationEvent)`
- [x] 5.2 Add `AttendeeRegisteredDomainEventHandler` → `IOutbox.Enqueue(AttendeeRegisteredIntegrationEvent)`
- [x] 5.3 Add `RegistrationCancelledDomainEventHandler` → `IOutbox.Enqueue(RegistrationCancelledIntegrationEvent)`
- [x] 5.4 Add `RegistrationReconfirmedDomainEventHandler` → `IOutbox.Enqueue(RegistrationReconfirmedIntegrationEvent)`
- [x] 5.5 Add `TicketsChangedDomainEventHandler` → `IOutbox.Enqueue(AttendeeTicketsChangedIntegrationEvent)`
- [x] 5.6 Add `TicketedEventStatusChangedDomainEventHandler` → switch on status, enqueue `TicketedEventCancelledIntegrationEvent` or `TicketedEventArchivedIntegrationEvent`
- [x] 5.7 Add `TicketedEventReconfirmPolicyChangedDomainEventHandler` → `IOutbox.Enqueue(TicketedEventReconfirmPolicyChangedIntegrationEvent)`
- [x] 5.8 Add `TicketedEventTimeZoneChangedDomainEventHandler` → `IOutbox.Enqueue(TicketedEventTimeZoneChangedIntegrationEvent)`
- [x] 5.9 Delete `RegistrationsMessagePolicy` and unused `CouponCreatedModuleEvent`

## 6. Delete Dead Shared Types

- [x] 6.1 Delete `IModuleEvent`, `ModuleEvent`
- [x] 6.2 Delete `IModuleEventHandler<T>`
- [x] 6.3 Delete `ModuleEventRouter`
- [x] 6.4 Delete `IMessagePolicy`, `MessagePolicy`, `MessagePolicyRule`, `MessagePolicyRuleBuilder<T>`
- [x] 6.5 Delete `OutboxWriter`
- [x] 6.6 Remove `AddModuleEventHandlersFromAssembly` registration calls and `AddMessagePolicy` / `AddKeyedScoped<IMessagePolicy>` wiring from all module DI setup

## 7. Tests

- [x] 7.1 Run architecture tests; fix any violations from deleted/renamed types
- [x] 7.2 Delete or update tests for removed classes (`MessagePolicy`, `OutboxWriter`, module event handlers, `RegistrationsMessagePolicyTests`)
- [x] 7.3 Add unit tests for each new domain event handler (verifies correct outbox message is enqueued)
- [x] 7.4 Verify existing integration/e2e tests still pass

## 8. Documentation

- [x] 8.1 Rewrite `docs/arc42/08-crosscutting-concepts.md` §8.6 (Messaging and outbox) to reflect the two-tier model (Command / IntegrationEvent) and updated handler patterns
- [x] 8.2 Write ADR documenting the decision to drop `ModuleEvent` and `MessagePolicy`, the rationale (VOs don't cross the wire, deferred command is clearer than intermediate event), and the alternatives considered
