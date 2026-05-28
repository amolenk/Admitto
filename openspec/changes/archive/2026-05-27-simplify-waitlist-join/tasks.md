## 1. Domain

- [x] 1.1 Rename `Waitlist.RequestJoin` to `Waitlist.AddEntry(email, addedAt)` — create the active entry directly and remove the verification-email domain event path
- [x] 1.2 Remove `Waitlist.ConfirmEntry` method
- [x] 1.3 Remove `WaitlistEntryAddedDomainEvent` (no longer published)

## 2. Application — Remove ConfirmWaitlistEntry

- [x] 2.1 Delete `ConfirmWaitlistEntryCommand.cs`
- [x] 2.2 Delete `ConfirmWaitlistEntryHandler.cs`
- [x] 2.3 Delete `ConfirmWaitlistEntryHttpEndpoint.cs` (PublicApi)
- [x] 2.4 Remove the confirm route registration from the Registrations public route builder

## 3. Application — Update JoinWaitlist

- [x] 3.1 Update `JoinWaitlistHttpRequest` — remove `email` field (email comes from the token); add `token` (string) field
- [x] 3.2 Update `JoinWaitlistValidator` — validate `token` is present and non-empty; remove email validation
- [x] 3.3 Update `JoinWaitlistHandler` — inject `IVerificationTokenService`; validate token; extract email from claims; call `waitlist.AddEntry(email, utcNow)` instead of `waitlist.RequestJoin(email)`
- [x] 3.4 Update `JoinWaitlistCommand` — replace `Email` with `Token`

## 4. Contracts & Integration Events

- [x] 4.1 Delete `WaitlistJoinRequestedIntegrationEvent.cs` from `Registrations.Contracts`
- [x] 4.2 Delete the Email module handler for `WaitlistJoinRequestedIntegrationEvent` (sends the old verification link email)
- [x] 4.3 Remove any outbox/inbox routing config for `WaitlistJoinRequestedIntegrationEvent`

## 5. Tests

- [x] 5.1 Remove domain tests for `Waitlist.RequestJoin` and `Waitlist.ConfirmEntry`
- [x] 5.2 Add domain tests for `Waitlist.AddEntry` (success, duplicate idempotency)
- [ ] 5.3 Update integration/API tests for `JoinWaitlist` — supply token, assert entry created immediately
- [x] 5.4 Remove integration/API tests for `ConfirmWaitlistEntry`
- [x] 5.5 Run full test suite (ArchTests first)
