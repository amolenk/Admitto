# ADR-001: Modular Monolith with Multiple Hosts

## Status
Accepted

## Context
Admitto consists of multiple functional areas with distinct responsibilities (modules). Each module has its own domain concepts, persistence needs, and integration concerns. At the same time, we value simplicity, fast feedback, and low operational overhead.

We want:
- Clear **module boundaries** to control coupling and support independent evolution.
- The ability to **separate runtime concerns**, such as HTTP request handling versus background processing.
- A deployment and development model that remains **simpler than microservices**, while still enforcing architectural discipline.

## Decision
- Implement Admitto as a **modular monolith**.
- Deploy the system using **multiple hosts/containers**:
  - **ApiHost**: hosts all HTTP endpoints (public and admin).
  - **WorkerHost**: hosts background processing such as outbox dispatching, messaging handlers, and scheduled work.
- Use a **single physical database** with **separate schemas per module** to:
  - Make module ownership of data explicit.
  - Prevent accidental coupling at the database level.
  - Enable future extraction of modules if required.
- Enforce module boundaries through:
  - Project structure: one main project per module (with `Domain/`, `Application/`, `Infrastructure/` folders) plus a separate Contracts project for the module's public surface.
  - Explicit inter-module communication via facades and DTOs.

## Rationale
Although this architecture requires careful identification and enforcement of module boundaries, it is still significantly simpler than a microservices architecture:

- **No distributed system overhead**: no network hops between modules, no need for service discovery, API gateways, or per-service observability stacks.
- **Simpler development workflow**: a single solution, unified debugging, and the ability to refactor across modules when needed.
- **Lower operational complexity**: fewer deployments, fewer failure modes, and simpler local development and CI pipelines.
- **Incremental evolution**: boundaries are explicit but not enforced by physical separation, allowing the architecture to evolve without premature optimization.

The **separation into API and Worker hosts** also enables **zero-downtime deployments**. The API host is stateless and can be rolling-updated with multiple instances running concurrently. The Worker host, however, must be stopped before updating because overlapping instances would run duplicate background jobs (outbox dispatch, scheduled work). By deploying them as separate containers, the API stays available while the Worker is briefly recycled — background processing pauses momentarily but user-facing traffic is uninterrupted.

Compared to a traditional monolith, the use of:
- multiple hosts,
- explicit module boundaries,
- and separate database schemas

provides many of the benefits commonly associated with microservices (clear ownership, isolation, scalability options) without incurring their full complexity.

## Consequences
### Positive
- Clear separation of concerns at both runtime (hosts) and data level (schemas).
- Independent scaling of API and background processing.
- Zero-downtime deployments: the stateless API host can be rolling-updated while the Worker host is briefly recycled.
- Stronger architectural discipline than a classic layered monolith.
- Easier future extraction of modules if business or scale requirements change.

### Negative
- Requires continuous discipline to respect module boundaries.
- Shared database still requires governance (e.g. no cross-schema queries in application code).
- Less isolation than true microservices in failure and deployment independence.
