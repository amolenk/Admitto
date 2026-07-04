## Why

Admins currently can see a list of registrations but have no way to drill into a single attendee to inspect their full details, activity history, or which emails were sent to them. Adding a dedicated attendee detail page closes this gap and provides the natural starting point for future per-attendee actions such as cancellation and ticket-type changes.

## What Changes

- **New page** in the Admin UI at `/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}` showing:
  - Attendee details: first name, last name, email, registration status, additional detail fields
  - Ticket types held by the attendee
  - Activity timeline sourced from a new `ActivityLog` projection (entries: Registered, Reconfirmed, Cancelled)
  - Emails sent to this attendee for this event (subject, type, status, sent-at)
  - Functional "Cancel registration" action (moved here from the registrations table)
  - Placeholder "Change ticket types" button
- **Registrations table** rows become clickable links navigating to the attendee detail page; the per-row cancel action is removed (it now lives on the detail page)
- **New backend endpoint** `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}` (Registrations module) returning full registration details including attendee fields, tickets, additional details, status, and activity log entries
- **New backend endpoint** `GET /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/emails` (Email module) returning emails sent to the attendee using the new `registrationId` column on `EmailLog` — no cross-module facade call needed
- **`registrationId` column** added to `email_log` table (nullable; populated for all single-send and bulk-send emails tied to a registration)
- **`ActivityLog` projection** added to the Registrations module: a new table of immutable timestamped entries projected from domain events (`AttendeeRegistered`, `RegistrationReconfirmed`, `RegistrationCancelled`); supports future re-registration scenarios where the same registration can cycle through multiple states

## Capabilities

### New Capabilities

- `admin-registration-detail`: Backend query + admin HTTP endpoint for fetching a single registration's full details (name, email, status, tickets, additional details, activity log entries)
- `activity-log`: New read-side projection in the Registrations module tracking lifecycle milestones (Registered, Reconfirmed, Cancelled) as immutable timestamped entries; projected from domain events; supports future re-registration scenarios
- `attendee-emails`: Backend query + admin HTTP endpoint for fetching emails sent to a specific attendee within an event, using the new `registrationId` column on `EmailLog` — no cross-module facade call needed
- `admin-ui-attendee-detail`: Admin UI attendee detail page showing all of the above, with a functional cancel action and a placeholder button for change ticket types

### Modified Capabilities

- `email-log`: Add nullable `registrationId` column to `email_log`; populate it for all sends tied to a registration (single-send and bulk fan-out)
- `admin-ui-registrations`: Registrations table rows gain a clickable link to the attendee detail page; the per-row cancel action is removed (it moves to the attendee detail page)

## Impact

- **Admitto.Module.Registrations**: New `GetRegistrationDetails` query/handler/DTO/endpoint; new `ActivityLog` entity + EF migration; new domain event handlers projecting into the activity log
- **Admitto.Module.Email**: New `GetAttendeeEmails` query/handler/DTO/endpoint; `EmailLog` gains nullable `RegistrationId`; `SendEmailCommand` gains optional `RegistrationId`; EF migration; bulk email sender passes `registrationId`
- **Admitto.UI.Admin**: New attendee detail page; updated registrations table (row links, cancel moved); new `apiClient` calls
- No breaking changes to existing API contracts; the `registrationId` column is nullable and existing rows keep null
