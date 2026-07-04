## 1. Domain — OTP Code aggregate

- [x] 1.1 Add `OtpCode` entity to `Admitto.Module.Registrations/Domain/` with fields: `Id`, `EmailHash`, `EventId`, `CodeHash`, `ExpiresAt`, `UsedAt` (nullable), `FailedAttempts`, `SupersededAt` (nullable), and a row-version concurrency token
- [x] 1.2 Add `OtpCode` domain logic: `bool IsExpired(DateTimeOffset now)`, `bool IsUsed`, `bool IsLocked` (FailedAttempts >= 5), `bool IsSuperseded`; method `MarkUsed()`, `IncrementFailedAttempts()`, `Supersede()`
- [x] 1.3 Add `OtpCodeRequestedDomainEvent` carrying `TeamId`, `EventId`, `RecipientEmail`, `OtpCode` (plain-text, transient — NOT persisted in outbox payload after delivery)

## 2. Infrastructure — OtpCode persistence

- [x] 2.1 Add `OtpCodeEntityConfiguration` (EF) and include in `RegistrationsDbContext`
- [x] 2.2 Add EF Core migration for the `OtpCodes` table
- [x] 2.3 Add `IOtpCodeRepository` (or use DbSet directly) with methods: `GetActiveCodesCountAsync(emailHash, eventId, since)`, `GetUnexpiredCodeAsync(emailHash, eventId)`, `SupersedeAllPendingAsync(emailHash, eventId)`

## 3. Infrastructure — Verification token service

- [x] 3.1 Add `IVerificationTokenService` interface to `Admitto.Module.Registrations` (or Contracts if used by public endpoints) with `string Issue(string email, Guid eventId, Guid teamId)` and `VerificationTokenClaims? Validate(string token, Guid eventId)`
- [x] 3.2 Implement `HmacVerificationTokenService` using `System.IdentityModel.Tokens.Jwt` (HS256), reading the signing key from configuration (`Registrations:VerificationToken:SigningKey`)
- [x] 3.3 Register `HmacVerificationTokenService` in the Registrations module DI setup

## 4. Application — Request OTP use case

- [x] 4.1 Add `RequestOtpCommand` (email, eventId, teamId) and `RequestOtpHandler` under `Application/UseCases/SelfService/RequestOtp/`
- [x] 4.2 Handler logic: hash email, count recent codes (rate limit: max 3 in 10 min), supersede existing pending codes, generate 6-digit OTP, store hashed, raise `OtpCodeRequestedDomainEvent`
- [x] 4.3 Add `RequestOtpHttpRequest`, `RequestOtpValidator` (email required, valid format), `RequestOtpHttpEndpoint` at `POST /events/{teamSlug}/{eventSlug}/otp/request` returning 202; resolve teamSlug/eventSlug to IDs via `IOrganizationFacade`; return 404 if event not found, 429 if rate limited

## 5. Application — Verify OTP use case

- [x] 5.1 Add `VerifyOtpCommand` (email, code, eventId) and `VerifyOtpHandler` under `Application/UseCases/SelfService/VerifyOtp/`
- [x] 5.2 Handler logic: hash email + code, look up unexpired code by emailHash+eventId, check locked/expired/used/code-match, increment failed attempts on mismatch (with optimistic concurrency), mark used on success, issue verification token via `IVerificationTokenService`
- [x] 5.3 Add `VerifyOtpHttpRequest`, `VerifyOtpValidator`, `VerifyOtpHttpEndpoint` at `POST /events/{teamSlug}/{eventSlug}/otp/verify` returning 200 `{token}` on success or 422 with generic message on failure

## 6. Application — Self-service registration endpoint

- [x] 6.1 Add `SelfRegisterHttpEndpoint` at `POST /events/{teamSlug}/{eventSlug}/register` under `Application/UseCases/SelfService/Register/Public/`
- [x] 6.2 Endpoint extracts Bearer token from `Authorization` header, validates via `IVerificationTokenService` (event binding + email match), returns 401 on failure
- [x] 6.3 Wire endpoint to existing `RegisterAttendeeCommand` / handler (the handler already exists per the attendee-registration spec); pass the verified email from the token through to the command
- [x] 6.4 Add `SelfRegisterHttpRequest` (attendeeInfo, tickets) and `SelfRegisterValidator`

## 7. Application — Self-service cancel use case

- [x] 7.1 Add `SelfCancelRegistrationCommand` (registrationId, eventId) and `SelfCancelRegistrationHandler` under `Application/UseCases/SelfService/CancelRegistration/`
- [x] 7.2 Handler logic: look up `Registration` by registrationId+eventId (404 if not found or wrong event), verify status is `Registered` (409 if Cancelled), cancel with `CancellationReason.AttendeeRequest`, release capacity, raise `RegistrationCancelledIntegrationEvent`
- [x] 7.3 Add `SelfCancelHttpEndpoint` at `POST /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/cancel`; no auth token needed — registration ID in path is the credential; wire to command

## 8. Application — Self-service change tickets use case

- [x] 8.1 Add `SelfChangeTicketsCommand` (registrationId, eventId, newTicketSlugs) and `SelfChangeTicketsHandler` under `Application/UseCases/SelfService/ChangeTickets/`
- [x] 8.2 Handler logic: look up `Registration` by registrationId+eventId (404 if not found or wrong event), verify `Registered` (409 if Cancelled), verify event Active + registration window Open + policy Open, validate new tickets (no dupes, no unknown, no cancelled, no overlapping slots), compute delta, `Release(toRelease)`, `Claim(toClaim, enforce: true)`, `registration.ChangeTickets(newTickets)`, raise `TicketsChangedDomainEvent`
- [x] 8.3 Add `SelfChangeTicketsHttpRequest`, `SelfChangeTicketsValidator`, `SelfChangeTicketsHttpEndpoint` at `PUT /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/tickets`; no auth token needed — registration ID in path is the credential; wire to command

## 9. Email — OTP delivery

- [x] 9.1 Add `OtpEmailTemplate` type to the Email module (or extend the existing template enum/type)
- [x] 9.2 Add integration event or module event handler in the Email module to listen for `OtpCodeRequestedDomainEvent` and enqueue an OTP delivery email via the existing outbox/SMTP pipeline using the platform SMTP (not per-event SMTP)
- [x] 9.3 Add OTP email template (subject: "Your verification code", body includes the 6-digit code and a short expiry notice)

## 10. API wiring

- [x] 10.1 Register the public self-service endpoint group in `RegistrationsModule.cs` or equivalent public endpoint entry point — OTP and register endpoints under `/events/{teamSlug}/{eventSlug}/`, cancel and change-tickets under `/events/{teamSlug}/{eventSlug}/registrations/{registrationId}/`
- [x] 10.2 Ensure public endpoints do NOT apply `ValidationFilter` admin auth or team-membership auth; they use token-based auth only
- [x] 10.3 Update `Admitto.Api` OpenAPI configuration if needed (tag/group self-service endpoints separately from admin endpoints)

## 11. Configuration

- [x] 11.1 Add `Registrations:VerificationToken:SigningKey` (min 32-byte secret) to `appsettings.json` / Aspire secrets — used only for the OTP-to-registration token
- [x] 11.2 Add `Registrations:VerificationToken:TokenTtlMinutes` (default 15) to configuration
- [x] 11.3 Add `Registrations:Otp:ExpiryMinutes` (default 10) and `Registrations:Otp:RateLimitWindow` (default 10 min, max 3 requests) to configuration

## 12. Tests

- [x] 12.1 Unit tests for `OtpCode` domain logic (expiry, lock, supersede, mark-used)
- [x] 12.2 Unit tests for `HmacVerificationTokenService` (issue + validate, expiry, wrong event binding)
- [x] 12.3 E2E tests for OTP request endpoint: SC001–SC005 from email-otp-verification spec
- [x] 12.4 E2E tests for OTP verify endpoint: SC006–SC011 from email-otp-verification spec
- [x] 12.5 E2E tests for self-service registration: Successful + token missing + token invalid + token wrong event scenarios
- [x] 12.6 E2E tests for self-service cancel: SC001–SC003 from self-service-cancel-registration spec (valid cancel, not found, already cancelled)
- [x] 12.7 E2E tests for self-service change tickets: SC001–SC007 from self-service-change-tickets spec
