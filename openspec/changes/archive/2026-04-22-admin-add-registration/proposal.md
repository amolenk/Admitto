## Why

Today, attendees can only join a ticketed event through public self-service registration or by redeeming a single-use coupon. Organizers regularly need to add registrations themselves — for speakers, sponsors, comp tickets, late additions after the registration window has closed, or attendees who can't complete the public flow (no email access at the time, payment handled out-of-band, etc.). Without an admin-add path, organizers either mint single-use coupons just to register one person (heavyweight and pollutes the coupon list) or skip recording the registration entirely (which breaks capacity, attendance lists, and emails).

## What Changes

- Add a new admin-only registration path that lets a team admin create a registration directly for a given attendee email and ticket selection on a specific event.
- Bypass the registration window, the ticket-type capacity limit, the requirement for ticket types to have an explicit capacity, and the email-domain restriction (admin override).
- Continue to enforce: event lifecycle status (Active only — reject Cancelled/Archived), duplicate-email guard, additional-detail schema validation, and ticket-selection rules (no duplicates, no unknown/cancelled types, no overlapping time slots).
- Continue to consume capacity (increment used counts) so attendance numbers stay accurate, even though the limit itself is not enforced.
- Expose this through:
  - An admin HTTP endpoint in `Admitto.Module.Registrations` under the existing `/admin/teams/{team}/events/{event}/...` route family.
  - A matching CLI command in `Admitto.Cli` (per the cli-admin-parity capability).
  - An Admin UI affordance on the event registrations page to add a registration via a dialog/page.

## Capabilities

### New Capabilities
- `admin-registration`: Admin-initiated registration creation for a ticketed event, including the policy-bypass rules, surface-area definition (admin endpoint + CLI), and validation that still applies.
- `admin-ui-registrations`: Admin UI surface for viewing/adding registrations on an event, scoped initially to the "add registration" flow.

### Modified Capabilities
None. The existing `attendee-registration` requirements already phrase the shared rules (ticket-selection validation, duplicate-email, additional-detail validation) generically across "all registration paths", and the existing `cli-admin-parity` requirement already mandates that any new admin endpoint gets a matching CLI command — both apply to admin-add-registration without spec changes.

## Impact

- **Domain**: No new aggregates. `Registration.Create` is reused. The existing `TicketCatalog.Claim(..., enforce: false)` overload (already used by coupons) is reused for the unenforced capacity claim.
- **Application**: New use case under `src/Admitto.Module.Registrations/Application/UseCases/Registrations/AdminRegisterAttendee/` containing the command, handler, and `AdminApi/` slice (endpoint, request, validator, response). Endpoint registered through the module's admin endpoint wiring.
- **API**: New `POST /admin/teams/{team}/events/{event}/registrations` endpoint, returning the created registration id. Authorisation via the existing admin policy.
- **CLI**: New `admitto registration add` command (or equivalent under the `event` branch) that calls the new endpoint via the regenerated `ApiClient`. Requires `generate-api-client.sh` to be re-run.
- **Admin UI**: Add an "Add registration" affordance on the event registrations page (reachable from the existing event navigation), opening a form that collects email, ticket-type selection, and any additional details defined by the event schema.
- **Tests**: New domain/application tests for the admin handler covering the bypassed and still-enforced rules; API endpoint tests under `tests/Admitto.Api.Tests`; UI build verification.
- **Docs**: Update `docs/arc42/06-runtime-view.md` if a new runtime flow is documented; otherwise no architecture change.
