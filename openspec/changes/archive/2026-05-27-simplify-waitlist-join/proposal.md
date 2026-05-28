## Why

The current waitlist join flow requires a separate email-verification step (HMAC-signed
link) that is redundant given attendees must already verify their email via the OTP flow
before performing any self-service action. The two-step confirm pattern adds unnecessary
complexity, an extra endpoint, and a second verification email.

## What Changes

- **BREAKING** The `POST /waitlist/{ticketTypeId}` (JoinWaitlist) endpoint now requires a
  valid OTP verification token in the request body. The entry is created immediately on
  join — no secondary confirmation is needed.
- **REMOVED** The `POST /waitlist/{ticketTypeId}/confirm` (ConfirmWaitlistEntry) endpoint
  is deleted.
- Removed: `WaitlistJoinRequestedIntegrationEvent` (the verification email trigger).
- Removed: `ConfirmWaitlistEntryCommand`, `ConfirmWaitlistEntryHandler`, and related
  infrastructure.
- The `Waitlist.RequestJoin` domain method is replaced by a direct `AddEntry` that
  validates the token and creates the entry in one step.
- The `WaitlistEntryAddedDomainEvent` is no longer used for triggering email
  verification; it may be retired or repurposed.

## Capabilities

### New Capabilities

*(none)*

### Modified Capabilities

- `waitlist`: The join requirement changes — a verification token is now required on
  join; the separate confirm step is removed.

## Impact

- `JoinWaitlistHttpEndpoint` — updated request shape (add `token` field)
- `JoinWaitlistHandler` — validate token using `IVerificationTokenService`, create entry immediately
- `ConfirmWaitlistEntryHttpEndpoint`, `ConfirmWaitlistEntryHandler`, `ConfirmWaitlistEntryCommand` — deleted
- `Waitlist` domain entity — `RequestJoin` → `AddEntry(email, position, addedAt)`; `ConfirmEntry` removed
- `WaitlistJoinRequestedIntegrationEvent` and its email handler in the Email module — deleted
- `WaitlistEntryAddedDomainEvent` — no longer raised for verification; may be removed
- Public API route registration — confirm route removed
- `JoinWaitlistValidator` — add token field validation
- Tests covering the old two-step confirm flow must be updated/removed
