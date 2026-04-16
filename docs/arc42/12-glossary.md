# 12. Glossary

| Term | Definition |
| :--- | :--------- |
| Aggregate | DDD building block; a cluster of entities treated as a unit for data changes, with a single root entity that controls access |
| Domain event | Event raised by an aggregate to represent a business state transition; dispatched synchronously within the same transaction |
| Module event | Asynchronous event derived from a domain event via a message policy; used for internal module-level or cross-module workflows |
| Integration event | Asynchronous event published as a public contract for external consumers; persisted in the outbox |
| Message policy | Per-module configuration that maps domain events to module events and/or integration events |
| Outbox | Transactional outbox table that stores pending module/integration events for reliable async dispatch |
| Unit of work | Transaction boundary that persists module changes and outbox messages; owned by the API endpoint, not the command handler |
| Write store | Module-owned persistence abstraction (e.g. `IOrganizationWriteStore`) exposing DbSets for aggregates; located in the module's `Infrastructure/` folder |
| Facade | Cross-module query interface published in a Contracts project; prevents direct DbContext access between modules |
| Module key | String identifier (e.g. `"Organization"`) used for keyed DI registration of module-specific services |
| Organization scope | Record (`OrganizationScope`) returned by `IOrganizationScopeResolver` that holds resolved team and event identity (slugs + IDs) |
| Attendee | A person who holds one or more tickets for a ticketed event |
| Feature slice | Organizational pattern where an endpoint, its request DTO, validator, and mapping live together under a single use case folder |
| Organizer | A team member with the Organizer role; can create events and manage attendees |
| Registration | The act of an attendee claiming a ticket for a ticketed event; subject to capacity enforcement |
| Team | A group of members (organizers) that collaborates on creating and managing ticketed events |
| Ticketed event | An event created by an organizer with one or more ticket types, each with a defined capacity |
