## Context

The waitlist join flow currently has two HTTP steps: the attendee POSTs to
`/waitlist/{ticketTypeId}` to request a slot, which triggers a verification email
with an HMAC-signed link; only after clicking that link does
`POST /waitlist/{ticketTypeId}/confirm` create the actual waitlist entry.

This two-step flow was designed before the OTP email-verification infrastructure
existed. Now every self-service attendee interaction (registration, ticket changes,
cancellation) already requires the attendee to prove email ownership via the
`POST /otp/request` → `POST /otp/verify` flow, which returns a signed JWT
verification token. Waitlist join is the only remaining flow that uses a separate
verification mechanism.

## Goals / Non-Goals

**Goals:**
- Accept a verification token (OTP-issued JWT) on the JoinWaitlist endpoint and create the entry immediately.
- Remove the separate ConfirmWaitlistEntry endpoint and all supporting code.
- Remove the `WaitlistJoinRequestedIntegrationEvent` and the email handler that sends the verification link.
- Keep the join idempotent: submitting with a valid token when already on the waitlist still returns success.

**Non-Goals:**
- Changing the OTP flow itself.
- Changing any other waitlist behaviour (notification, coupon expiry, re-notify, WaitlistOnly mode lifecycle).
- Adding admin-side forced entry (separate concern).

## Decisions

### Token validation reuses IVerificationTokenService

The OTP verify endpoint already issues a short-lived HMAC-signed JWT containing
`email`, `eventId`, `teamId`, and `exp`. `IVerificationTokenService.Validate(token,
eventId)` already validates the signature, expiry, and event scope, returning a
`VerificationTokenClaims` with the verified email.

**Decision**: Inject `IVerificationTokenService` into `JoinWaitlistHandler` and call
`Validate` the same way `ConfirmWaitlistEntryHandler` does today. No new token type or
service is needed.

### Entry is created immediately in JoinWaitlist

Because the token already proves email ownership, `Waitlist.RequestJoin` (which only
raises an event to trigger a verification email) is replaced by `Waitlist.AddEntry`
which directly adds the active entry at the next queue position. The domain event
`WaitlistEntryAddedDomainEvent` is removed (no longer meaningful; nothing listens to
it).

### ConfirmWaitlistEntry is deleted entirely

There is no migration path for in-flight "pending" entries since the previous
`RequestJoin` path never persisted an entry — it only raised a domain event to send
an email. Any attendee who received the old verification link but hasn't clicked it
will simply need to rejoin using their OTP token. Since this is a new/beta feature
the deployment risk is acceptable.

### WaitlistJoinRequestedIntegrationEvent removed

The Email module's handler for this event sent the verification link email. With the
confirm step removed, the event serves no purpose and is deleted along with the handler.

## Risks / Trade-offs

- **In-flight verification links become invalid on deploy** → Acceptable: the feature
  is new and any affected attendees will see a clear 404/expired error and can rejoin.
- **Token TTL (15 min) is shorter than the old verification link (24 h)** → By design:
  the attendee must have a fresh OTP token to join, the same as for registration. This
  is consistent with all other self-service actions.
- **No waitlist entry exists until token is presented** → Pre-existing behaviour
  actually didn't persist a pending entry either; the only change is the confirmation
  step no longer exists.
