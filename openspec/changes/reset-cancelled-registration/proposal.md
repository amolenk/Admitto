## Why

Attendees who cancel currently cannot sign up again with the same email address because registrations are unique per event and email. Re-registration should be supported without deleting historical identity or bypassing the existing registration/capacity/email side effects.

## What Changes

- Change duplicate-email handling on attendee self-service, coupon, and admin-add registration paths.
- Allow a new registration request to reuse an existing `Cancelled` registration for the same event and email by resetting that registration back to `Registered`.
- Keep the existing duplicate-email rejection for an already `Registered` registration.
- Re-run the same policy, ticket selection, capacity claim, coupon, additional-detail, and active-event checks that a new registration would run before resetting the cancelled registration.
- Replace the cancelled registration's attendee details, ticket snapshot, additional details, cancellation metadata, and reconfirmation state with the new request data.
- Preserve the existing `RegistrationId` so admin/detail links and audit references remain stable.
- Emit the normal attendee-registered side effects after reset so confirmation email and activity-log flows behave like a new registration.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `attendee-registration`: Self-service and coupon registration requests with an event/email that matches a cancelled registration should reset that registration instead of being rejected as duplicate.
- `admin-registration`: Admin-add registration requests with an event/email that matches a cancelled registration should reset that registration instead of being rejected as duplicate.

## Impact

- `Admitto.Module.Registrations` domain model: `Registration` needs a reset/reactivation behavior.
- `Admitto.Module.Registrations` registration handler: duplicate-email lookup must distinguish `Registered` from `Cancelled` rows and reuse cancelled rows after all guards pass.
- Registrations persistence: the unique `(event_id, email)` index remains valid and should continue to protect concurrent duplicate writes.
- Registrations messaging: reset should produce the same attendee-registered integration event as a new registration.
- Tests: domain tests and registrations integration tests need new scenario coverage for reset and unchanged duplicate-active rejection.
