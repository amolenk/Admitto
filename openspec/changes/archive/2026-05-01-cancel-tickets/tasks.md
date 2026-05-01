## Tasks

### Group 1 — Domain cleanup

- [x] **T01** Verify no rows with `CancellationReason = 3` (Other) in the database. If any exist, map them before proceeding.
- [x] **T02** Rename `RequestForVisaLetter = 1` → `VisaLetterDenied = 1` in `CancellationReason.cs`. Remove `Other = 3`. Fix all compilation breaks. `TicketTypesRemoved = 2` is unchanged.

### Group 2 — Backend use case

- [x] **T03** Create `CancelRegistrationCommand(RegistrationId, TeamSlug, EventSlug, CancellationReason)`.
- [x] **T04** Create `CancelRegistrationHandler`: resolves event via `IRegistrationsWriteStore`, verifies team/event ownership, verifies registration belongs to event, calls `registration.Cancel(reason)`. Returns 404 if not found or wrong event, 409 if already cancelled.
- [x] **T05** Create `CancelRegistrationRequestValidator`: `reason` is required and must be `AttendeeRequest` or `VisaLetterDenied` (rejects `TicketTypesRemoved`).
- [x] **T06** Create `CancelRegistrationHttpEndpoint`: `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/cancel`. Wire validator as endpoint filter. Register in `RegistrationsModule`.

### Group 3 — Email template types

- [x] **T07** Add `Cancellation = "cancellation"` and `VisaLetterDenied = "visa-letter-denied"` constants to `EmailTemplateType` in `Admitto.Module.Email`.

### Group 4 — Default email templates

- [x] **T08** Port `canceled.txt` and `canceled.html` from `Admitto.Application/Common/Email/Templating/Defaults/` into `Admitto.Module.Email` as `cancellation.txt` / `cancellation.html`. Add as embedded resources.
- [x] **T09** Port `visa-letter-denied.txt` and `visa-letter-denied.html` from the same legacy location. Add as embedded resources.
- [x] **T10** Register both new template defaults in the email template service (alongside existing `ticket` and `reconfirm` defaults).

### Group 5 — Email handler

- [x] **T11** Create `RegistrationCancelledIntegrationEventHandler` in `Admitto.Module.Email/Application/UseCases/SendEmail/EventHandlers/`. Routes `Reason` → `EmailTemplateType` (`AttendeeRequest` → `cancellation`, `VisaLetterDenied` → `visa-letter-denied`, `TicketTypesRemoved` → no-op). Calls `IRegistrationsFacade.GetTicketedEventEmailContextAsync(ticketedEventId, registrationId)` for all template context. Dispatches `SendEmailCommand` with idempotency key `registration-cancelled:{registrationId}`. Skips if no email config.
- [x] **T12** Register the handler in `Admitto.Module.Email` DI.

### Group 6 — Tests

- [x] **T13** Handler unit tests: success with `AttendeeRequest`, success with `VisaLetterDenied`, 409 already cancelled, 404 not found, 404 wrong event, 400 invalid reason (`TicketTypesRemoved`).
- [x] **T14** Email handler unit tests: correct template per reason, `TicketTypesRemoved` is no-op, idempotency (second call is no-op), no email config skips send.
- [x] **T15** E2E test: admin cancels registration via HTTP endpoint, integration event fires, email arrives in MailDev.

### Group 7 — CLI

- [x] **T16** ~~skipped — CLI is legacy~~
- [x] ~~**T16**~~ Regenerate `ApiClient.g.cs` after endpoint is wired.
- [x] **T17** ~~Create `CancelRegistrationCommand` CLI.~~ (skipped — CLI is legacy)

### Group 8 — Admin UI

- [x] **T18** Regenerate Admin UI OpenAPI SDK.
- [x] **T19** Add per-row "Cancel" action (visible only for `Registered` rows) on the registrations list page. Confirmation dialog with two-option reason selector. Confirm button disabled until reason selected. Call `apiClient` on confirm, refresh list on success, show error notification on failure.
