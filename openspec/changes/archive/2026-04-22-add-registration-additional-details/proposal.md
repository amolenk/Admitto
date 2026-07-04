## Why

Event organizers often need to ask attendees for information beyond name/email — e.g., dietary requirements, t-shirt size, company name. The legacy codebase supported this via per-event "additional details", but the modular monolith currently has no equivalent, forcing organizers to either omit the questions or collect them out-of-band.

## What Changes

- Each `TicketedEvent` exposes an ordered, configurable schema of **additional detail fields**. Each field has a `Name` (unique within the event), a `MaxLength`, and a stable `Key` used for storage.
- All additional details are **always optional** at the platform layer; the event's public website is responsible for prompting users for any fields it considers mandatory and for any non-string validation. Admitto only enforces presence of declared keys and `MaxLength`.
- All additional detail values are stored as **strings**.
- Public registration (and the public `GetAvailability`/event-info endpoint) accept and surface an `additionalDetails` map keyed by the field's `Key`. Unknown keys are rejected; declared-but-omitted keys are stored as empty/missing.
- The admin UI gains an editor for the additional-detail schema on the existing **registration policy page**, supporting add / rename / reorder / remove / change-max-length.
- Removing a field from the schema **preserves historical values** on existing registrations (read-only in admin views) but stops collecting that field on new registrations.
- Admin attendee/registration views display the additional details (current schema fields plus any preserved historical fields).
- The CLI gains parity commands for managing the schema and for inspecting additional details on registrations (within the existing parity scope; CLI work blocked on the broader ApiClient regeneration drift may be tracked as a follow-up).

## Capabilities

### New Capabilities

- `registration-additional-details`: Per-event schema of optional, string-typed additional detail fields, including their lifecycle (add/rename/reorder/remove), storage of values on registrations, preservation of historical values when a field is removed, and CLI parity for managing the schema.

### Modified Capabilities

- `event-management`: `TicketedEvent` owns the additional-detail schema and emits domain/integration events when it changes.
- `attendee-registration`: Self-registration accepts an `additionalDetails` map (validated against the event's schema and `MaxLength`); registrations store these values; admin views surface them.
- `admin-ui-event-policies`: The registration policy page gains a section for editing the additional-detail schema.

## Impact

- **Backend (Registrations module)**: New value object for the schema and an `AdditionalDetails` value object on the registration aggregate. Public registration command + endpoint accept the new map. EF migration adds storage (JSON column) for additional details on registrations.
- **Backend (Organization module)**: `TicketedEvent` aggregate gains the additional-detail schema with admin command/handler for updating it; new domain/integration events.
- **Public API**: `GET` event-info / availability response includes the schema; self-registration `POST` accepts `additionalDetails`.
- **Admin API**: New endpoint to update the additional-detail schema; existing attendee/registration responses include collected values.
- **Admin UI**: Registration policy page extended with the schema editor; attendee/registration detail views show collected values.
- **CLI**: New commands for getting/updating the schema and inspecting collected values (subject to the existing ApiClient regeneration follow-up).
- **Specs**: New `registration-additional-details` capability; deltas for `event-management`, `attendee-registration`, `admin-ui-event-policies`.
