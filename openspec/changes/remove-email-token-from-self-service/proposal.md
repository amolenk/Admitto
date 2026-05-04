## Why

The self-service cancel and change-tickets endpoints currently validate a bearer email-verification token before proceeding, but the existing specs already state that the `registrationId` in the URL path is the sole bearer credential. This is a code-vs-spec mismatch: the implementation is stricter than intended, unnecessarily forcing attendees to re-verify their email every time they cancel or change tickets.

## What Changes

- Remove bearer-token extraction and `IVerificationTokenService.Validate` calls from `SelfCancelRegistrationHttpEndpoint`.
- Remove bearer-token extraction and `IVerificationTokenService.Validate` calls from `SelfChangeTicketsHttpEndpoint`.
- Remove the now-unused `IVerificationTokenService` and `HttpRequest` dependencies from both endpoints.
- Return HTTP 204 (cancel) / 200 (change tickets) based solely on `registrationId` authorization, consistent with the spec.

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `self-service-cancel-registration`: Code correction — existing spec already requires no auth token; delta spec documents the alignment fix.
- `self-service-change-tickets`: Code correction — existing spec already requires no auth token; delta spec documents the alignment fix.

## Impact

- `SelfCancelRegistrationHttpEndpoint.cs` — remove token validation logic
- `SelfChangeTicketsHttpEndpoint.cs` — remove token validation logic
- No API contract changes (path, method, request/response body are unchanged)
- No database migrations required
- Existing tests that pass a bearer token will need updating; no new test scenarios required beyond what the specs already define
