## 1. OTP Allowed-Domain Enforcement

- [x] 1.1 In `RequestOtpHandler`, after the active-event check and before rate limiting, enforce the event's allowed email domain via `TicketedEvent.EnsureEmailDomainAllowed`.
- [x] 1.2 Normalize the email to lowercase before computing the hash used for rate-limit and supersede lookups, matching `OtpCode.Create`.
- [x] 1.3 Add API tests: disallowed domain returns 400, allowed domain returns 202, and an unrestricted event accepts any domain.

## 2. Partner Event-Details Slice

- [x] 2.1 Add `GetPartnerTicketedEventDetails` query/handler and a reduced `PartnerTicketedEventDetailsDto` (name, slug, start/end, timezone, isRegistrationOpen, allowedEmailDomain, additionalDetailFields[key,name,maxLength]).
- [x] 2.2 Do not expose internal id, team id, version, lifecycle status, reconfirm policy, or waitlist policy.
- [x] 2.3 Add `GET /api/events/{eventSlug}` Partner endpoint reusing `PartnerTicketedEventResolver`; return 200 with the DTO or 404 when the slug does not resolve.
- [x] 2.4 Wire the endpoint into `RegistrationsModule.MapRegistrationsPartnerEndpoints` without changing the admin event-detail endpoint.

## 3. Tests

- [x] 3.1 Add API tests covering metadata + ordered field mapping, empty field list, and null allowed domain.
- [x] 3.2 Add API tests covering unknown slug (404) and missing API key (401).
- [x] 3.3 Add a response-shape assertion that admin-only fields are absent from the Partner payload.

## 4. Verification

- [ ] 4.1 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` and fix any architecture violations first.
- [ ] 4.2 Run the targeted Registrations API test suites changed by this work.
- [ ] 4.3 Regenerate the OpenAPI spec / Admin UI SDK to keep it in sync with the new endpoint.
