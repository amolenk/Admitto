## Why

Dynamic partner event websites drive attendee registration through the Partner API. Two gaps block a clean integration:

1. Events can restrict registration to an allowed email domain, but that rule is only enforced when an attendee submits a registration. An attendee can still request (and receive) an OTP for a disallowed address, wasting an email and giving no early signal to the event site.
2. The event site needs event metadata — the display name, timezone, registration-open state, the configured allowed email domain, and the additional-detail fields it must render — but no Partner API endpoint exposes it. The admin event-detail endpoint over-exposes internal fields and is team-membership scoped, so it cannot be reused.

## What Changes

- Enforce the event's allowed-email-domain restriction at OTP request time. Requests for a disallowed domain return `400 Bad Request` (`registration.email_domain_not_allowed`) before consuming the attendee's OTP rate-limit budget. Events without a restriction are unaffected.
- Fix a latent bug in the OTP request handler: the rate-limit/supersede lookup hashed the email without lowercasing, while stored codes hash the lowercased email, so mixed-case addresses bypassed both. The handler now lowercases before hashing.
- Add `GET /api/events/{eventSlug}` under the existing Partner API route family, returning a reduced, event-site-facing view: name, slug, start/end, timezone, registration-open flag, allowed email domain, and the additional-detail field schema (key, name, maxLength). No internal id, team id, version, lifecycle status, reconfirm policy, or waitlist policy is exposed.

## Capabilities

### New Capabilities
- `partner-event-details`: Partner API retrieval of a scoped, reduced ticketed-event metadata payload (including additional-detail field schema) for trusted event websites.

### Modified Capabilities
- `email-otp-verification`: OTP request now rejects email addresses whose domain is not allowed for the event.

## Impact

- Registrations module: new read-only `GetPartnerTicketedEventDetails` query/handler/DTO slice and Partner API endpoint; `RequestOtpHandler` gains a domain guard and email-hash normalization.
- Endpoint registration under `GET /api/events/{eventSlug}` in `RegistrationsModule.MapRegistrationsPartnerEndpoints`.
- API tests for the OTP domain guard and the new Partner event-details endpoint.
- No database schema changes.
