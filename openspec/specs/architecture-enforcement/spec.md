# Architecture Enforcement

## Purpose

Executable architecture rules for `Admitto.Core` enforced via ArchUnitNET. The `Admitto.Core.ArchTests` project encodes every structural convention — dependency direction, class naming, and placement — as MSTest tests that run in under 10 seconds with no infrastructure. Agents MUST run these tests before any functional tests.

## Requirements

### Requirement: Module dependency direction is enforced
Cross-module dependencies SHALL only flow through each module's `Contracts` namespace. No module's `Domain`, `Application`, or `Infrastructure` namespaces may be referenced from outside that module.

#### Scenario: Application code references another module's Domain
- **WHEN** a class in `Admitto.Core.Module.Registrations.Application` references a type in `Admitto.Core.Module.Organization.Domain`
- **THEN** the ArchUnit test `Module application layer must not reference other module domain layers` SHALL fail

#### Scenario: Application code references another module's Contracts
- **WHEN** a class in `Admitto.Core.Module.Registrations.Application` references a type in `Admitto.Core.Module.Organization.Contracts`
- **THEN** all ArchUnit tests SHALL pass

### Requirement: Domain layer has no upward dependencies
A module's `Domain` namespace SHALL NOT depend on that same module's `Application` or `Infrastructure` namespaces, nor on any other module's namespaces.

#### Scenario: Domain references Application
- **WHEN** a domain entity references a type from its own module's `Application` namespace
- **THEN** the ArchUnit test `Domain layer must not depend on Application or Infrastructure` SHALL fail

#### Scenario: Domain references another module's Domain
- **WHEN** a type in `Admitto.Core.Module.Organization.Domain` references a type in `Admitto.Core.Module.Registrations.Domain`
- **THEN** the ArchUnit test `Domain layer must not depend on other modules` SHALL fail

### Requirement: Shared.Kernel has no internal dependencies
`Admitto.Core.Shared.Kernel` SHALL NOT depend on any other `Admitto.Core` namespace.

#### Scenario: Shared.Kernel references Shared.Application
- **WHEN** a type in `Admitto.Core.Shared.Kernel` references a type in `Admitto.Core.Shared.Application`
- **THEN** the ArchUnit test `Shared.Kernel must have no Admitto.Core dependencies` SHALL fail

### Requirement: Infrastructure does not depend on other modules
A module's `Infrastructure` namespace SHALL NOT reference another module's `Domain`, `Application`, or `Infrastructure` namespaces (only `Contracts` is permitted).

#### Scenario: Infrastructure references another module's Application
- **WHEN** a class in `Admitto.Core.Module.Email.Infrastructure` references a type from `Admitto.Core.Module.Registrations.Application`
- **THEN** the ArchUnit dependency rule SHALL fail

### Requirement: Domain event handlers are correctly named
Classes implementing `IDomainEventHandler<T>` SHALL be named exactly `{T.Name}Handler`.

#### Scenario: Handler named after its event type
- **WHEN** a class implements `IDomainEventHandler<RegistrationCancelledDomainEvent>`
- **THEN** the class name SHALL be `RegistrationCancelledDomainEventHandler`

#### Scenario: Handler with incorrect name
- **WHEN** a class implements `IDomainEventHandler<T>` but its name does not equal `{T.Name}Handler`
- **THEN** the ArchUnit test `Domain event handlers must follow naming convention` SHALL fail

### Requirement: Integration event handlers are correctly named
Classes implementing `IIntegrationEventHandler<T>` SHALL be named exactly `{T.Name}Handler`.

#### Scenario: Integration handler named after its event type
- **WHEN** a class implements `IIntegrationEventHandler<AttendeeRegisteredIntegrationEvent>`
- **THEN** the class name SHALL be `AttendeeRegisteredIntegrationEventHandler`

### Requirement: Module event handlers are correctly named
Classes implementing `IModuleEventHandler<T>` SHALL be named exactly `{T.Name}Handler`.

#### Scenario: Module handler named after its event type
- **WHEN** a class implements `IModuleEventHandler<UserAddedModuleEvent>`
- **THEN** the class name SHALL be `UserAddedModuleEventHandler`

### Requirement: Command handlers are correctly named
Classes implementing `ICommandHandler<TCommand>` SHALL be named by replacing the `Command` suffix of `TCommand.Name` with `Handler` (e.g. `CreateApiKeyCommand` → `CreateApiKeyHandler`).

#### Scenario: Command handler named after its command
- **WHEN** a class implements `ICommandHandler<CreateApiKeyCommand>`
- **THEN** the class name SHALL be `CreateApiKeyHandler`

### Requirement: Query handlers are correctly named
Classes implementing `IQueryHandler<TQuery, TResult>` SHALL be named by replacing the `Query` suffix of `TQuery.Name` with `Handler`.

#### Scenario: Query handler named after its query
- **WHEN** a class implements `IQueryHandler<GetApiKeysQuery, ...>`
- **THEN** the class name SHALL be `GetApiKeysHandler`

### Requirement: Event handler classes reside in EventHandlers namespaces
All `*DomainEventHandler`, `*IntegrationEventHandler`, and `*ModuleEventHandler` classes SHALL reside in a namespace whose last segment is `EventHandlers`.

#### Scenario: Event handler in correct namespace
- **WHEN** `RegistrationCancelledDomainEventHandler` resides in `...UseCases.ReleaseTickets.EventHandlers`
- **THEN** all ArchUnit placement tests SHALL pass

#### Scenario: Event handler in wrong namespace
- **WHEN** an event handler class does not reside in a namespace ending with `EventHandlers`
- **THEN** the ArchUnit test `Event handlers must reside in EventHandlers namespace` SHALL fail

### Requirement: HTTP endpoints reside in AdminApi or PublicApi namespaces
Classes whose name ends with `HttpEndpoint` SHALL reside in a namespace whose last segment is `AdminApi` or `PublicApi`.

#### Scenario: Endpoint in AdminApi namespace
- **WHEN** `CreateApiKeyHttpEndpoint` resides in `...UseCases.ApiKeyManagement.CreateApiKey.AdminApi`
- **THEN** all ArchUnit placement tests SHALL pass

#### Scenario: Endpoint outside API namespace
- **WHEN** an `*HttpEndpoint` class does not reside in a namespace ending with `AdminApi` or `PublicApi`
- **THEN** the ArchUnit test `HttpEndpoints must reside in AdminApi or PublicApi namespace` SHALL fail

### Requirement: Validators reside alongside endpoints
`AbstractValidator<T>` subclasses used as endpoint validators SHALL reside in the same `AdminApi` or `PublicApi` namespace as the endpoint they serve.

#### Scenario: Validator in correct namespace
- **WHEN** `CreateApiKeyValidator` resides in the same `...AdminApi` namespace as `CreateApiKeyHttpEndpoint`
- **THEN** all ArchUnit placement tests SHALL pass

### Requirement: Commands and queries reside in UseCases namespaces
Classes whose name ends with `Command` or `Query` SHALL reside in a namespace containing `Application.UseCases`.

#### Scenario: Command in correct namespace
- **WHEN** `CreateApiKeyCommand` resides in `...Module.Organization.Application.UseCases.ApiKeyManagement.CreateApiKey`
- **THEN** all ArchUnit placement tests SHALL pass

### Requirement: Architecture tests run before functional tests
The agent workflow SHALL always execute `dotnet test tests/Admitto.Core.ArchTests` before `dotnet test tests/Admitto.Core.Tests`. ArchUnit tests require no running infrastructure and complete in under 10 seconds.

#### Scenario: Structural violation caught before running unit tests
- **WHEN** an agent places a new event handler in the wrong namespace
- **THEN** `dotnet test tests/Admitto.Core.ArchTests` SHALL fail with a precise message before any unit test is executed
