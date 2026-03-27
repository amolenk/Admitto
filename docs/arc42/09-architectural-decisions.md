# 9. Architectural decisions

Detailed ADRs are stored in [`/docs/adrs/`](../adrs/).

| Date | Decision | Status | ADR |
| :--- | :------- | :----- | :-- |
| — | Modular monolith with multiple hosts | Accepted | [ADR-001](../adrs/adr-001-modular-monolith.md) |
| — | Minimal APIs with feature-sliced endpoint organization | Accepted | [ADR-002](../adrs/adr-002-minimal-api.md) |
| — | EF Core with PostgreSQL | Accepted | [ADR-003](../adrs/adr-003-ef-core-postgresql.md) |
| — | Azure Storage Queues for async messaging | Accepted | [ADR-004](../adrs/adr-004-azure-storage-queues.md) |
| — | Capability gating for host-specific handlers | Accepted | [ADR-005](../adrs/adr-005-capability-gating.md) |

## Done-when

- [x] A scan-friendly timeline table exists.
- [x] Each entry has at least the decision and a short motivation.
- [ ] Decisions with real trade-offs have considered options recorded.
- [ ] Decisions link to where they show up (chapters 4–8).
