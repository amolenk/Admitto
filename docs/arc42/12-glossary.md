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
| Organization scope | The resolved team and event identity (IDs) extracted from route parameters and used in API handlers and authorization |
| Attendee | A person who holds one or more tickets for a ticketed event |
| Feature slice | Organizational pattern where an endpoint, its request DTO, validator, and mapping live together under a single use case folder |
| Organizer | A team member with the Organizer role; can create events and manage attendees |
| Registration | The act of an attendee claiming a ticket for a ticketed event; subject to capacity enforcement |
| Reconfirmation policy | An optional ticketed-event policy that defines the window, minimum interval between reminder emails, and optional reconfirmation quiet hours for attendee reconfirmation. |
| Reconfirmation cycle | The one-time reconfirmation period for a registration. A new registration or reset/reregistration after cancellation starts a fresh cycle; it ends when the attendee reconfirms or the registration is cancelled. |
| Effective maximum reconfirmation emails | The smallest optional maximum configured among a registration's ticket types for its current cycle. A missing maximum does not constrain the registration, and only successfully delivered reconfirmation emails count. |
| Reconfirmation quiet hours | An optional event-local period during which routine reconfirmation evaluation and reminder delivery are deferred. |
| Reconfirmation auto-expiry | The hourly evaluation outcome that auto-cancels an unreconfirmed registration once it reaches its effective maximum reconfirmation emails, using the normal cancellation flow and side effects. |
| Team | A group of members (organizers) that collaborates on creating and managing ticketed events |
| Ticketed event | An event created by an organizer with one or more ticket types, each with a defined capacity |
| Waitlist quiet hours | The event-wide period in the waitlist policy that extends waitlist-offer claim deadlines. It does not control reconfirmation evaluation or reminder delivery. |
