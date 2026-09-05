# 9. Architectural decisions

Detailed ADRs are stored in [`/docs/adr/`](../adr/).

| Date | Decision | Status | ADR |
| :--- | :------- | :----- | :-- |
| — | Modular monolith with multiple hosts | Accepted | [ADR-001](../adr/adr-001-modular-monolith.md) |
| — | Minimal APIs with feature-sliced endpoint organization | Accepted | [ADR-002](../adr/adr-002-minimal-api.md) |
| — | EF Core with PostgreSQL | Accepted | [ADR-003](../adr/adr-003-ef-core-postgresql.md) |
| — | Azure Storage Queues for async messaging | Superseded by ADR-015 | [ADR-004](../adr/adr-004-azure-storage-queues.md) |
| — | Capability gating for host-specific handlers | Accepted | [ADR-005](../adr/adr-005-capability-gating.md) |
| — | Admin UI technology stack | Accepted | [ADR-006](../adr/adr-006-admin-ui-technology-stack.md) |
| — | Lifecycle guard pattern in the Registrations module | Superseded by ADR-008 | [ADR-007](../adr/adr-007-lifecycle-guard-pattern.md) |
| — | TicketedEvent ownership moved to Registrations; EventStatus projected onto TicketCatalog | Accepted | [ADR-008](../adr/adr-008-ticketed-event-ownership-in-registrations.md) |
| — | Bulk-email fan-out: snapshot recipients, single SMTP connection, per-event time zone | Accepted | [ADR-009](../adr/adr-009-bulk-email-design.md) |
| 2026-06-10 | Keycloak as the production identity provider | Accepted | [ADR-012](../adr/adr-012-keycloak-production-identity-provider.md) |
| 2026-06-26 | Platform SMTP sender, public event links, and team-owned email branding | Accepted | [ADR-013](../adr/adr-013-platform-sender-public-links.md) |
| 2026-06-27 | Email-owned event rendering context projection | Accepted | [ADR-014](../adr/adr-014-email-event-context-projection.md) |
| 2026-08-16 | Azure Service Bus with push-based consumption | Accepted | [ADR-015](../adr/adr-015-service-bus-push-based-consumption.md) |
| 2026-08-27 | Hourly reconfirmation evaluation replacing per-event triggers | Accepted | [ADR-016](../adr/adr-016-hourly-reconfirmation-evaluation.md) |

## Done-when

- [x] A scan-friendly timeline table exists.
- [x] Each entry has at least the decision and a short motivation.
- [ ] Decisions with real trade-offs have considered options recorded.
- [ ] Decisions link to where they show up (chapters 4–8).
