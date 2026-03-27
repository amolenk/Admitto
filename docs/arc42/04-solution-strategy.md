# 4. Solution strategy

The core strategic decisions:

- **Single deployable system, explicit module boundaries.** Admitto is a modular monolith (ADR-001), not microservices. Modules are isolated through project structure, separate database schemas, and facade-based cross-module communication — but they ship and deploy together.

- **Separate runtime hosts for separate concerns.** HTTP request handling (API host), background processing (Worker host), and schema migrations (Migrations host) run as distinct processes. This lets them scale and fail independently without the complexity of a service mesh.

- **DDD-inspired layering inside modules.** Each module follows Domain → Application → Infrastructure → Contracts layering. Domain models own business rules; application handlers orchestrate use cases; infrastructure implements persistence and external integrations.

- **Transactional outbox for reliable async messaging.** Domain events are captured during `SaveChanges`, mapped to module/integration events via message policies, and persisted in the same database transaction. Dispatch happens best-effort immediately, with background retry for failures. Azure Storage Queues serve as the dispatch target ([ADR-004](../adrs/adr-004-azure-storage-queues.md)).

- **EF Core directly from the application model.** No repository abstraction layer — handlers and use cases work directly with `DbContext`. This fits feature-slicing and keeps data access simple. PostgreSQL backs the storage with schema-per-module isolation ([ADR-003](../adrs/adr-003-ef-core-postgresql.md)).

- **Transport layer owns the transaction boundary.** API endpoints — not command handlers — call `SaveChangesAsync`. This keeps handlers framework-agnostic and lets the endpoint decide when to commit (see [chapter 8](08-crosscutting-concepts.md)).
