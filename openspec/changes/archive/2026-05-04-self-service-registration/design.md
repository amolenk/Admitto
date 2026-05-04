## Context

The Registrations module supports self-service attendee registration via public endpoints. The `attendee-registration` spec already mandates that self-service registration requires a signed email-verification token, but the mechanism for *obtaining* that token does not exist. Additionally, once registered, attendees have no self-service path to cancel or change their tickets — they must contact an organizer.

This design adds:
1. An OTP request/verify flow that issues a short-lived verification token used only for the initial self-service registration
2. Public self-service endpoints for cancellation and ticket changes, authenticated by the attendee's **registration ID**

The key insight: OTP verification proves email ownership *before* a registration exists. Once registered, the attendee knows their registration ID (a GUID returned by the register endpoint). That ID is unguessable and acts as a sufficient bearer credential for subsequent operations — no repeated OTP dance required.

All new flows are public (no admin/team auth) and live under `/events/{teamSlug}/{eventSlug}/`.

## Goals / Non-Goals

**Goals:**
- Provide OTP-based email ownership verification producing a reusable short-lived token scoped to the initial registration step
- Wire the public self-service registration endpoint (spec already exists; implementation is missing); return the registration ID in the response
- Add public self-service cancel and change-tickets endpoints authenticated by registration ID (no OTP required post-registration)

**Non-Goals:**
- Full attendee account management or persistent sessions
- Coupon-based self-service change/cancel (coupon registrations follow a separate redemption path; admin manages them)
- Admin UI changes for the new public flows (attendees interact with an external event site, not the Admin UI)
- Changing admin cancel/change-tickets endpoints (they stay as-is)

## Decisions

### Decision 1: HMAC-signed JWT as the verification token (not an IdP session)
Verification tokens are self-contained HMAC-signed JWTs (HS256) issued by the Registrations module, signed with a secret key from configuration. They embed `email`, `eventId`, `teamId`, and `exp`. No external identity provider is involved.

**Rationale:** Attendees do not have accounts; verification is per-action, not persistent. HMAC JWTs are stateless, require no extra storage, and can be validated in-process. Alternative: opaque tokens stored in DB would require an extra lookup on every request and add storage pressure for high-throughput events.

**Alternative considered:** Reuse Keycloak with a device-flow/magic-link. Rejected: too heavyweight, requires provisioning per-attendee accounts, and ties the public registration UX to the admin IdP.

### Decision 2: OTP codes stored (hashed) in the Registrations module DB
A new `OtpCode` entity is added to the Registrations module. It stores: `Id`, `EmailHash` (SHA-256 of email, lowercased), `EventId`, `CodeHash` (SHA-256 of the 6-digit code), `ExpiresAt`, `UsedAt`, and `FailedAttempts`. On verification success, `UsedAt` is set (single-use). On verification failure, `FailedAttempts` is incremented.

**Rationale:** Codes must be single-use and rate-limited. Stateless alternatives (e.g., TOTP) require a shared secret stored per user, which is harder to manage without attendee accounts. Storing a hashed code in the existing Registrations DB is simple and consistent with the module's persistence strategy.

### Decision 3: OTP delivery via the Email module's outbox
When an OTP is requested, the Registrations module raises a domain event (`OtpCodeRequestedDomainEvent`). The Email module handles this event and sends the OTP email via the existing outbox/SMTP pipeline. This avoids a synchronous cross-module call and keeps email infrastructure centralized.

**Alternative considered:** Direct SMTP call from Registrations. Rejected: violates the Email module's ownership of SMTP configuration and delivery. The Email module must own all transactional email.

**Note:** OTP emails are delivered through the single shared email pipeline used for all transactional email in the system.

### Decision 4: Registration ID as the bearer credential for post-registration operations
Cancel and change-tickets endpoints accept the registration ID in the URL path (`/events/{teamSlug}/{eventSlug}/registrations/{registrationId}/cancel` and `.../tickets`). No additional token is required.

**Rationale:** A GUID registration ID is cryptographically unguessable (128 bits of entropy). The attendee receives it in the successful registration response and can store it. This avoids repeated OTP flows for routine post-registration actions (change of mind, cancellation), which would be poor UX. It is analogous to a "magic link" — possession of the ID proves authorization.

**Security note:** Registration IDs must be generated with a cryptographically secure random source (standard `Guid.NewGuid()` on .NET uses OS entropy and is sufficient). The ID is returned only once (in the register response) and is not displayed in the admin UI in a way that would leak it to non-owner admins (admin endpoints use the same ID but admins are already trusted).

**Alternative considered:** Re-use the OTP/verification-token approach for cancel/change too. Rejected: requires attendees to go through the OTP flow every time they want to cancel or change tickets — poor UX for a simple operation.

### Decision 5: Self-service ticket change enforces capacity; self-service cancel uses AttendeeRequest reason
- **Cancel**: Public cancel endpoint transitions the registration to `Cancelled` with `CancellationReason.AttendeeRequest`. No new reason value is needed.
- **Change tickets**: The `claim` call uses `enforce: true` (capacity-enforced), unlike admin change-tickets which uses `enforce: false`. Attendees cannot overclaim capacity.

**Rationale:** Admin tools exist for exceptional situations. Attendees operate within normal constraints.

### Decision 6: Rate limiting — per email+event, 3 OTP requests per 10 minutes, 5 failed verify attempts before lockout
OTP request rate limiting: A new request invalidates all previous pending codes for the same email+event. Maximum 3 OTP emails per email+event per 10-minute window (enforced by counting unexpired codes). Failed verify attempts up to 5 are allowed before the code is locked (no further attempts even before expiry).

**Rationale:** Prevents enumeration and email-bombing. Simpler than external rate-limiting middleware.

## Risks / Trade-offs

- [Risk: OTP email delivery failure] → OTP delivery depends on the email pipeline being healthy. Mitigation: same resilience measures that apply to all transactional email (outbox pattern, retry).
- [Risk: Registration ID reuse/enumeration] → UUIDs are not enumerable. Rate-limiting on cancel/change endpoints (e.g. 10 requests/minute per IP) provides additional protection against brute-force.
- [Risk: Concurrent OTP requests could confuse attendees] → New request invalidates previous codes. Attendees always use the most recent code.
- [Risk: `FailedAttempts` race condition under concurrent verify attempts] → `OtpCode` uses EF optimistic concurrency (row version) to prevent concurrent increments from double-decrementing the remaining attempts.

## Migration Plan

1. Add `OtpCode` EF entity and migration to the Registrations module DB
2. Register new public endpoint group in `RegistrationsModule.cs` / public endpoint wiring
3. Deploy Worker alongside API (no new infrastructure; OTP emails flow through existing outbox)
4. No data migration required; existing registrations are unaffected

## Open Questions

- Should self-service change-tickets be gated by the registration window (same as initial registration) or allowed any time the event is Active? (Current decision: gated by registration window — same policy as initial registration.)
- Is there a need for a "view my registration" public endpoint (returning registration details)? (Out of scope for this change; can be added as a follow-up.)
