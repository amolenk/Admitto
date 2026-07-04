## Why

Email composition currently depends on synchronous cross-module reads for slow-changing team and event context. Registrations assembles `EventRegistrationSnapshotDto` for Email and calls Organization for team branding, which blurs ownership and adds per-email facade calls for data that Email can safely consume as an eventually consistent rendering projection.

## What Changes

- Add an Email-owned, persisted event email context projection containing only the team/event facts needed for email rendering and reconfirm scheduling.
- Keep the projection synchronized from Organization and Registrations integration events instead of querying Organization for team branding during email context assembly.
- Replace transactional email handlers' `GetEventRegistrationSnapshotAsync` dependency with Email-owned projection reads plus facts already present on integration-event payloads.
- Keep live Registrations facade calls for attendee lists, reconfirm candidate eligibility, and bulk-email recipient resolution where current specs require live registration state.
- Remove `IOrganizationFacade.GetTeamBrandingAsync` once Email no longer needs synchronous branding reads.
- Update architecture docs to describe the Email-owned rendering projection and its eventual-consistency semantics.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `email-sending`: transactional email composition uses Email-owned team/event rendering context synchronized from integration events instead of per-send synchronous Organization/Registrations context reads.
- `reconfirm-sending`: reconfirm trigger scheduling uses the Email-owned event context projection for event time zone and policy snapshots, while candidate eligibility remains evaluated against live Registrations data.
- `bulk-email`: bulk fan-out can enrich built-in/system template rendering with Email-owned event context while attendee-source recipient resolution remains a live Registrations facade query at job resolution time.

## Impact

- Affected modules: Email, Organization, and Registrations.
- Affected contracts: Organization integration events for team branding changes, Registrations integration events for event email context changes, and removal of the Organization branding facade method.
- Affected persistence: new Email schema projection table and EF migration.
- Affected docs: arc42 module/runtime/cross-cutting sections and possibly an ADR if the projection is considered a durable architectural decision.
- No external HTTP API contract changes are expected.
