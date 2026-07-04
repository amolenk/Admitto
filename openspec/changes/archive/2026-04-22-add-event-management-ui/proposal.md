## Why

Admins currently have no UI to manage ticketed events; they must use the CLI. We want the same tabbed settings experience as teams, but events span multiple modules (Organization owns the event, Registrations owns the registration policy, and a new Email module will own per-event email server settings). We also need to enforce a cross-module invariant: an event must not be opened for registration unless its email is configured, so attendees always receive confirmation mail.

## What Changes

- Add an **Admin UI for event management** with a tabbed layout mirroring the team settings pattern:
  - **General** tab — event name, slug (read-only), start/end dates (Organization module)
  - **Registration** tab — registration window, capacity, email domain restriction, ticket types, open/close registration action (Registrations module)
  - **Email** tab — SMTP/provider settings for per-event outbound email (new Email module)
- Introduce a new **Email module** (`Admitto.Module.Email` + `Admitto.Module.Email.Contracts`) owning per-event email server settings, with:
  - Admin endpoints to get/update email settings for an event
  - A facade contract (`IEventEmailFacade`) exposing `IsEmailConfiguredAsync(eventId)` for other modules
- Modify **registration-policy** so opening an event for registration SHALL fail when the Email module reports that email is not configured for that event. Registrations calls the Email facade synchronously during the "open registration" command.
- Add a create-event page and wire event switching/navigation in the admin UI so event-level pages are reachable from the team's events list.

## Capabilities

### New Capabilities
- `admin-ui-event-management`: Admin UI pages for creating events and editing event settings across the General / Registration / Email tabs, including validation, optimistic concurrency, and navigation.
- `email-settings`: New Email module owning per-event email server settings (storage, admin endpoints, and a cross-module facade that reports whether email is configured for a given event).

### Modified Capabilities
- `registration-policy`: Opening registration SHALL require that the Email module reports email as configured for the event; the "open registration" operation returns a validation error otherwise.

## Impact

- **New module**: `src/Admitto.Module.Email/` and `src/Admitto.Module.Email.Contracts/`, registered in `Admitto.Api`, `Admitto.Worker`, and `Admitto.Migrations`.
- **Organization module**: admin endpoints for updating event general settings (if not already present for all fields needed by the UI).
- **Registrations module**: `OpenRegistration` use case gains a dependency on `IEventEmailFacade`; new admin endpoints to expose ticket types and registration policy settings for the UI.
- **Admin UI** (`src/Admitto.UI.Admin`): new event create page, event settings pages with tabs, event switcher/sidebar updates.
- **CLI** (`src/Admitto.Cli`): add commands for any new admin endpoints (per repo convention).
- **Database**: new schema `email` with an `event_email_settings` table.
- **Docs**: update `docs/arc42/05-building-block-view.md` to add the Email module; add an ADR if needed for cross-module facade checks.

## Follow-up clarification — trust the eventing pipeline

During implementation we removed the implicit "auto-create the policy if it's missing" branches that several Registrations command handlers had grown. Those branches conflated two concerns ("does the event exist?" vs. "can we act on it now?") and let bugs in the cross-module sync masquerade as successful operations.

The corrected design:

- `OrganizationMessagePolicy` publishes a new `TicketedEventCreatedModuleEvent` from `TicketedEventCreatedDomainEvent`, mirroring the existing Cancelled/Archived flows.
- A new Registrations use case `EventLifecycleSync/HandleEventCreated/` consumes that module event and creates the `EventRegistrationPolicy` (`EventLifecycleStatus = Active`, `RegistrationStatus = Draft`). The handler is idempotent so retries are safe.
- Every Registrations handler that requires the policy now looks it up and throws `EventRegistrationPolicy.Errors.EventNotFound` (`ErrorType.NotFound`, surfaced as HTTP 404 by the existing problem-details pipeline) when it is absent. No handler creates a policy on the fly anymore.

