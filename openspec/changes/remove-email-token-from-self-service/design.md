## Context

Two self-service endpoints have a code-vs-spec mismatch:

- `POST /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/cancel`
- `PUT /events/{teamSlug}/{eventSlug}/registrations/{registrationId}/tickets`

Both endpoints currently inject `IVerificationTokenService` and extract a `Bearer` token from the `Authorization` header, returning HTTP 401 if absent or invalid. The existing specs for both capabilities already state: *"The `registrationId` in the URL path serves as the bearer credential. No additional authentication token is required."* The implementation is incorrect; this design documents the fix.

## Goals / Non-Goals

**Goals:**
- Bring `SelfCancelRegistrationHttpEndpoint` and `SelfChangeTicketsHttpEndpoint` into conformance with their existing specs.
- Remove all bearer-token extraction logic and `IVerificationTokenService` usage from these two endpoints.

**Non-Goals:**
- Changing the URL, method, request body, or response shape of either endpoint.
- Modifying any other endpoint (admin cancel, admin change tickets, self-register, etc.).
- Reviewing whether `IVerificationTokenService` is used elsewhere — other usages are out of scope.

## Decisions

**Decision: No new authorization mechanism needed**
The `registrationId` is a GUID that is unguessable by third parties and was issued at registration time. The spec treats possession of the ID as sufficient proof of authorization. No replacement security layer is required.

*Alternative considered*: Require the attendee's email address as a body field for confirmation. Rejected — adds friction without improving security materially, and is not in the spec.

**Decision: Remove `HttpRequest` injection alongside token logic**
`HttpRequest` is injected solely to extract the `Authorization` header. Once token logic is removed, this parameter has no remaining purpose and should also be removed to keep handler signatures lean.

## Risks / Trade-offs

- **[Risk] Existing tests pass a bearer token** → Tests that currently supply a valid token will need updating to omit it; no behavioral regression since the endpoint will now succeed without one.
- **[Risk] Confusion about removed security** → Mitigated by the spec already being authoritative; the `registrationId` GUID provides adequate security by obscurity for self-service flows.
