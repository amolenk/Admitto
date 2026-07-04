# ADR-005: Capability Gating for Host-Specific Handlers

## Status
Accepted

## Context
Admitto uses a modular monolith with multiple hosts ([ADR-001](adr-001-modular-monolith.md)). Both the API host and Worker host load the same module assemblies so they share domain logic, application services, and infrastructure code.

However, some command handlers depend on infrastructure that is only available in a specific host. For example, email-sending handlers require SMTP access, which is configured only in the Worker host. Registering these handlers in the API host would cause runtime failures when the required dependencies cannot be resolved.

We need a mechanism to selectively activate handlers based on the host they run in, without splitting handlers into separate assemblies or introducing host-specific projects.

## Decision
Use an attribute-based capability gating mechanism:

- Define a `[Flags]` enum `HostCapability` that lists available infrastructure capabilities (e.g. `Email`).
- Annotate handlers that need host-specific infrastructure with `[RequiresCapability(HostCapability.Email)]`.
- At startup, each host declares which capabilities it supports by passing a `HostCapability` value to the assembly scanning registration.
- During DI registration, the assembly scanner checks each handler's `[RequiresCapability]` attribute. If the handler has no attribute it is always registered. If it has one, it is only registered when the host's declared capabilities satisfy the requirement (checked via bitwise AND).

Handlers without the attribute are registered in all hosts.

## Rationale
- **No assembly splitting**: both hosts can reference the same module project. This avoids an explosion of host-specific projects.
- **Fail-safe by default**: handlers without the attribute work everywhere. Only handlers with explicit infrastructure dependencies need annotation.
- **Compile-time discoverability**: the attribute is visible in the handler source, making it easy to understand why a handler only runs in certain hosts.
- **Extensible**: new capabilities (e.g. `Scheduling`) can be added to the flags enum without changing the filtering logic.

## Consequences
### Positive
- Email handlers are only registered in the Worker host, preventing missing-dependency errors in the API host.
- Module assemblies stay shared across hosts, keeping the project structure simple.
- Adding new host-specific capabilities requires only a new enum flag and attribute annotation.

### Negative
- Misconfigured capabilities are only caught at DI resolution time (missing service), not at compile time.
- The mechanism is limited to command handlers registered via assembly scanning. Other service types need different gating if host-specific.
