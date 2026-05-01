## Why

Admins need to cancel individual registrations on behalf of attendees (e.g. no-shows, late withdrawals, visa-letter denials). No admin cancel endpoint exists in the current module system. The cancellation email templates exist in the legacy project and must be migrated to the Email module.

## What Changes

- **BREAKING** — `CancellationReason` enum is rationalised: `RequestForVisaLetter` is renamed to `VisaLetterDenied` (integer value unchanged) and the unused `Other` value is removed. `TicketTypesRemoved` is kept as an internal/system-only value (used by a future "cancel ticket types" feature).
- New admin HTTP endpoint `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/cancel` with a required `reason` body field (`AttendeeRequest` or `VisaLetterDenied` only). `TicketTypesRemoved` is rejected by validation.
- New Email module handler for `RegistrationCancelledIntegrationEvent` (already carries `TicketedEventId`, `RegistrationId`, `RecipientEmail`, `Reason`): calls `IRegistrationsFacade.GetTicketedEventEmailContextAsync` to get all template context in one call, then dispatches a `SendEmailCommand`. Routes `AttendeeRequest` → `cancellation` template; `VisaLetterDenied` → `visa-letter-denied` template. `TicketTypesRemoved` is a no-op (handled by a future change).
- `RegistrationCancelledIntegrationEvent` requires **no changes** — the existing fields are sufficient.
- Two email template types added to the Email module (`cancellation`, `visa-letter-denied`) with built-in defaults migrated from the legacy project.
- New CLI command `admitto event registration cancel <id> --reason <reason>`.
- Admin UI: per-row "Cancel" action with a two-option reason selector on the registrations list page.

## Capabilities

### New Capabilities

- `admin-cancel-registration`: Admin cancels an individual registration with reason `AttendeeRequest` or `VisaLetterDenied`. Rejected when already cancelled.

### Modified Capabilities

- `admin-ui-registrations`: Per-row "Cancel" action with a reason selector.
- `email-sending`: Email module handles `RegistrationCancelledIntegrationEvent` and dispatches the appropriate cancellation email.
- `email-templates`: Two new template types: `cancellation` and `visa-letter-denied`.

## Impact

- **Admitto.Module.Registrations** — `CancellationReason` rationalised; new `CancelRegistration` use case.
- **Admitto.Module.Email** — new `EmailTemplateType` values; new `RegistrationCancelledIntegrationEventHandler`; two new built-in default templates.
- **Admitto.Cli** — new `cancel` sub-command under `event registration`.
- **Admitto.UI.Admin** — cancel affordance with two-option reason selector.
- **Schema / data** — pre-change check confirms no rows carry `Other = 3` before deployment.
