## Why

The registration module has specs for self-service registration with email-verification tokens but no mechanism for attendees to request/verify OTP codes to obtain those tokens. Without this, public registration, self-service ticket changes, and self-service cancellation cannot be implemented end-to-end. Adding these public flows unblocks event-driven attendee management without requiring admin involvement for every action.

## What Changes

- New public endpoint to **request an OTP code** sent to an attendee's email address (rate-limited, event-scoped)
- New public endpoint to **verify an OTP code** and receive a short-lived signed JWT used only for the initial registration step
- New public endpoint for **self-service registration** (existing spec already mandates the verification token; this adds the concrete HTTP endpoint and handler wiring)
- New public endpoint for **self-service ticket change** — attendee changes their ticket selection using their **registration ID** as the credential
- New public endpoint for **self-service registration cancellation** — attendee cancels using their **registration ID** as the credential
- The OTP flow is scoped to initial registration only; for subsequent operations the registration ID (a GUID, known only to the attendee after registration) serves as the bearer credential

## Capabilities

### New Capabilities
- `email-otp-verification`: Request and verify OTP codes for email ownership; on successful verification returns a short-lived signed token used exclusively for the initial self-service registration
- `self-service-change-tickets`: Public endpoint for attendees to change their ticket-type selection on an existing registration, authenticated by registration ID
- `self-service-cancel-registration`: Public endpoint for attendees to cancel their own registration, authenticated by registration ID

### Modified Capabilities
- `attendee-registration`: The verification token requirement and self-service registration scenarios are already specified; add the missing scenarios for the OTP token lifetime, event-scoping, and the registration ID returned to the attendee on success

## Impact

- **Registrations module**: New `OtpCode` entity, new `VerificationToken` service, new public endpoints in `Admitto.Api`; registration ID must be included in the successful registration response
- **Email module**: OTP delivery email must be sent via the existing email infrastructure (new email template type)
- **Public API surface**: New public routes under `/events/{teamSlug}/{eventSlug}/` for otp-request, otp-verify, register; and `/events/{teamSlug}/{eventSlug}/registrations/{registrationId}/` for cancel and change-tickets
- **Security**: OTP codes must be short-lived (e.g. 10 minutes), single-use, and rate-limited per email/event; verification tokens must be signed (HMAC or asymmetric) and similarly short-lived (e.g. 15 minutes); registration IDs are UUIDs and act as unguessable bearer credentials
- No breaking changes to existing admin endpoints
