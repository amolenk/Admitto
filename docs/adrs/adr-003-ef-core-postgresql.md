# ADR-003: EF Core with PostgreSQL

## Status

Accepted

## Context

Admitto needs a persistence layer that supports transactional writes, domain event interception, and schema-per-module isolation. The team evaluated several approaches to data access and database selection.

## Decision

- Use **EF Core** as the persistence layer, accessed directly from the application model (no repository abstraction).
- Use **PostgreSQL** as the backing relational database.
- Assign each module its own PostgreSQL **schema** to enforce data ownership boundaries.

## Rationale

**EF Core directly from the application model:**

- Fits naturally with feature-sliced organization — handlers and use cases work directly with `DbContext` and `DbSet<T>`.
- EF Core's interceptor pipeline enables cross-cutting concerns (domain event dispatch via `DomainEventsInterceptor`, audit trail via `AuditInterceptor`) without custom plumbing.
- The dependency on EF Core is acceptable: it still provides loose coupling from the actual database implementation through its provider model. Swapping PostgreSQL for another relational database would not require rewriting application logic.
- A repository abstraction layer would add indirection without meaningful benefit at this scale.

**PostgreSQL:**

- Schema-per-module isolation makes module data ownership explicit and prevents accidental cross-module coupling at the database level.
- Reliable, well-supported, and has strong JSON and concurrency features.
- Single database instance keeps operational complexity low while schemas provide logical separation.

## Consequences

### Positive

- Simple, low-ceremony data access aligned with feature slicing.
- Interceptors enable powerful cross-cutting behavior (events, auditing) declaratively.
- Schema isolation enforces module boundaries without separate database instances.
- EF Core migrations provide repeatable schema management per module.

### Negative

- Application code has a direct dependency on EF Core types (`DbContext`, `DbSet<T>`).
- Schema-per-module requires governance to prevent cross-schema queries in application code.
- PostgreSQL-specific features (e.g., `IPostgresExceptionMapping`) create some coupling to the database vendor.
