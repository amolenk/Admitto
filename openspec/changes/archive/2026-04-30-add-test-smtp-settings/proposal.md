## Why

Organizers can configure team- and event-scoped SMTP settings from the Admin UI, but they have no way to verify that the configuration actually works. Today, the only signal that the settings are wrong is a failed real registration email — which surfaces only in the `email_log` table, well after the misconfiguration was introduced. A one-click "Send test email" gives organizers immediate feedback (success or the underlying SMTP error) so they can iterate on credentials before opening a real event for registration.

## What Changes

- Add a synchronous **send-test-email** admin endpoint to the Email module's settings slice family at both team and event scope. The endpoint loads the saved settings for the requested scope (no fallback to the team scope), sends a small fixed-content diagnostic message via `IEmailSender`, and surfaces SMTP/connection errors back to the caller.
- The endpoint accepts the recipient address in the request body. The Email module does not look up team membership or the team's contact email — the Admin UI resolves the recipient client-side from data it already fetches, so no new cross-module call is introduced.
- Add a "Send test email" action to the Admin UI on both the team email settings page and the event email Settings tab. The action shows a recipient dropdown populated from the team's contact email and the team's member emails (already available via existing `/api/teams/{teamSlug}` and `/api/teams/{teamSlug}/members` endpoints). The action is enabled only when settings have been saved at the current scope; result and errors render inline.
- Add a matching CLI command (`admitto email settings test --team <slug> [--event <slug>] --recipient <addr>`) so the new admin endpoint has CLI parity.
- The diagnostic send is **not** recorded in `email_log` (it is not real correspondence) and does **not** go through the message outbox — the user receives the result of the send synchronously.

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `email-settings`: Add a requirement for the team- and event-scoped send-test-email admin endpoint, including recipient passed in the request body, scope isolation (no fallback), error surfacing, and the rule that diagnostic sends are excluded from `email_log`.
- `admin-ui-team-email-settings`: Add a requirement that the team email settings page exposes a "Send test email" action with a recipient dropdown (team contact email + team member emails) and inline success/error feedback when saved settings exist.
- `admin-ui-event-management`: Add a requirement that the event Email tab exposes a "Send test email" action with the same recipient dropdown and inline feedback when event-scoped settings exist.
- `cli-admin-parity`: Add a scenario for the new `email-settings test` admin endpoint to keep CLI parity explicit.

## Impact

- **Email module** (`src/Admitto.Module.Email`): new `SendTestEmail` slice (command, handler, request DTO, validator, admin endpoint) under `Application/UseCases/EmailSettings/SendTestEmail/`; `MapEmailAdminEndpoints` extended to register the new route on both team and event groups.
- **Admin UI** (`src/Admitto.UI.Admin`): new `TestEmailSettingsButton` component used by both `teams/[teamSlug]/settings/email/page.tsx` and `teams/[teamSlug]/events/[eventSlug]/settings/email/page.tsx` (each page additionally fetches team + members to populate the recipient options); new Next.js proxy routes under `app/api/.../email-settings/test/route.ts` for both scopes; regenerated SDK (`pnpm run openapi-ts`).
- **CLI** (`src/Admitto.Cli`): new `email settings test` command under `Commands/Email/Settings/`; regenerated NSwag `ApiClient.g.cs`.
- **Tests**: new handler unit tests and admin API integration tests for the test endpoint at both scopes.
- **No** changes to `IOrganizationFacade`, `OrganizationFacade`, `CachingOrganizationFacade`, the existing send pipeline (`SendEmailCommandHandler`, `EffectiveEmailSettingsResolver`), DB schema/migrations, data protection, outbox, or background jobs.
