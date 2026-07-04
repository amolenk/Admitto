## 1. Shared Infrastructure — Type-Erased Dispatch

- [x] 1.1 Create `MessageTypeRegistryBuilder` in `Shared.Infrastructure.Messaging`: fluent `AddCommand<T>()` and `AddIntegrationEvent<T>()` methods; `Build()` returns a frozen `MessageTypeRegistry`
- [x] 1.2 Refactor `MessageTypeRegistry` to accept an explicit type dictionary from the builder instead of scanning assemblies; remove assembly constructor
- [x] 1.3 Add `AddMessageTypeRegistry(Action<MessageTypeRegistryBuilder>)` extension to `SharedInfrastructureExtensions` that builds and registers the singleton
- [x] 1.4 Update `DomainEventsInterceptor` to resolve `IDomainEventHandler<T>` via `IServiceProvider.GetServices()` + `MakeGenericType` directly; remove `IMediator` constructor dependency; preserve OpenTelemetry activity span
- [x] 1.5 Update `QueueMessageDispatcher` to resolve `ICommandHandler<T>` via `IServiceProvider` + `MakeGenericType` directly; remove `IMediator` constructor dependency

## 2. Organization Module

- [x] 2.1 Register all Organization command and query handlers as concrete scoped services (`services.AddScoped<CreateTeamHandler>()` etc.); register domain event handlers as `IDomainEventHandler<T>`; do this inside the existing DI file as preparation
- [x] 2.2 Update `OrganizationFacade` to inject `GetTeamMembershipRoleHandler` and `ValidateApiKeyHandler` as concrete types; remove `IMediator` dependency
- [x] 2.3 Update all Organization HTTP endpoints to inject their concrete command/query handler instead of `IMediator`
- [x] 2.4 Update Organization integration event handlers (`TicketedEventCreatedIntegrationEventHandler` etc.) to inject concrete command handlers instead of `IMediator`
- [x] 2.5 Create `AddOrganizationModule(this IHostApplicationBuilder)` collapsing `AddOrganizationApplicationServices` + `AddOrganizationInfrastructureServices` + `AddOrganizationIdentityServices` into one call with explicit handler registrations
- [x] 2.6 Create `AddOrganizationModuleWorker(this IHostApplicationBuilder)` that calls `AddOrganizationModule()` then adds: keyed `IIntegrationEventHandler<T>` registrations, `IntegrationEventSubscriber` marker, `ICommandHandler<RegisterExternalUserCommand>` registration, and Quartz job setup
- [x] 2.7 Add `AddOrganizationMessageTypes(this MessageTypeRegistryBuilder)` method registering `RegisterExternalUserCommand` and all Organization integration event types

## 3. Registrations Module

- [x] 3.1 Register all Registrations command and query handlers as concrete scoped services; register domain event handlers as `IDomainEventHandler<T>`
- [x] 3.2 Update `RegistrationsFacade` to inject concrete query handler types (`GetTicketedEventEmailContextHandler`, `QueryRegistrationsHandler`, etc.) instead of `IMediator`
- [x] 3.3 Update all Registrations HTTP endpoints to inject concrete command/query handlers instead of `IMediator`
- [x] 3.4 Create `AddRegistrationsModule(this IHostApplicationBuilder)` collapsing Application + Infrastructure into one call
- [x] 3.5 Create `AddRegistrationsModuleWorker(this IHostApplicationBuilder)` that calls `AddRegistrationsModule()` then adds keyed `IIntegrationEventHandler<T>` for `TicketedEventCreationRequestedIntegrationEvent` and `IntegrationEventSubscriber` marker
- [x] 3.6 Add `AddRegistrationsMessageTypes(this MessageTypeRegistryBuilder)` registering all Registrations integration event types

## 4. Email Module

- [x] 4.1 Register all Email command and query handlers as concrete scoped services; register domain event handlers as `IDomainEventHandler<T>`; register `SendEmailHandler` as concrete scoped (not behind `ICommandHandler<T>` — it is never queue-dispatched directly)
- [x] 4.2 Update Email integration event handlers that currently call `mediator.SendAsync(sendEmailCommand)` (e.g., `AttendeeRegisteredIntegrationEventHandler`) to inject `SendEmailHandler` concretely
- [x] 4.3 Update Email integration event handlers that currently call `mediator.SendAsync(scheduleReconfirmationsCommand)` to inject `ScheduleReconfirmationsHandler` concretely
- [x] 4.4 Update all Email HTTP endpoints to inject concrete command/query handlers instead of `IMediator`
- [x] 4.5 Create `AddEmailModule(this IHostApplicationBuilder)` collapsing Application + Infrastructure into one call
- [x] 4.6 Create `AddEmailModuleWorker(this IHostApplicationBuilder)` that calls `AddEmailModule()` then adds: keyed `IIntegrationEventHandler<T>` registrations for all 9 Email integration event handlers, `IntegrationEventSubscriber` marker, `ICommandHandler<TriggerBulkEmailJobCommand>` registration, and Email + Quartz job setup (`ReconcileReconfirmationSchedulingStartupService`, Quartz jobs)
- [x] 4.7 Add `AddEmailMessageTypes(this MessageTypeRegistryBuilder)` registering `TriggerBulkEmailJobCommand` and all Email integration event types

## 5. Remove Mediator and Scrutor

- [x] 5.1 Delete `IMediator` interface and `Mediator.cs` from `Shared.Application.Messaging`
- [x] 5.2 Delete `RequiresCapabilityAttribute.cs` and `HostCapability` enum from `Shared.Application.Messaging`
- [x] 5.3 Remove `AddCommandHandlersFromAssembly`, `AddQueryHandlersFromAssembly`, `AddDomainEventHandlersFromAssembly`, `AddIntegrationEventHandlersFromAssembly`, and the `TryAddEnumerableStrategy` helper from `SharedApplicationExtensions`
- [x] 5.4 Remove `AddMessagingApplicationServices()` from `SharedApplicationExtensions` (only registered `IMediator`)
- [x] 5.5 Remove Scrutor `<PackageReference>` from `Admitto.Core.csproj`

## 6. Update Host Entry Points

- [x] 6.1 Update `Admitto.Api/Program.cs` to call `AddOrganizationModule()`, `AddRegistrationsModule()`, `AddEmailModule()` and remove old multi-call patterns
- [x] 6.2 Update `Admitto.Worker/Program.cs` to call `AddOrganizationModuleWorker()`, `AddRegistrationsModuleWorker()`, `AddEmailModuleWorker()`, and `AddMessageTypeRegistry(types => { types.AddOrganizationMessageTypes(); ... })`

## 7. Verify

- [x] 7.1 Run architecture tests: `dotnet test tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` — update any assertions that reference removed types
- [x] 7.2 Build the full solution and resolve any remaining compilation errors
- [x] 7.3 Run the full test suite; fix any failures caused by missing handler registrations or changed DI setup
