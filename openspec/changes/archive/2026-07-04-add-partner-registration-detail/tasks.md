## 1. Application Slice

- [x] 1.1 Add a dedicated Registrations query/handler slice for reading one registration by resolved team ID, event ID, and registration ID.
- [x] 1.2 Add a Partner-specific response DTO that contains only id, email, firstName, lastName, status, current ticket selection, and additionalDetails.
- [x] 1.3 Ensure the query maps current ticket identifiers from the registration's stored ticket snapshot and returns an empty additionalDetails dictionary when no values exist.

## 2. Partner API Endpoint

- [x] 2.1 Add `GET /api/events/{eventSlug}/registrations/{registrationId}` under the Registrations Partner API surface.
- [x] 2.2 Reuse the existing Partner event resolver so the endpoint derives TeamId from `X-Api-Key` authentication and resolves the event slug within that team scope before dispatching the query.
- [x] 2.3 Return 200 with the reduced DTO when found and not found when either the event or scoped registration cannot be resolved.
- [x] 2.4 Wire the endpoint into `RegistrationsModule.MapRegistrationsPartnerEndpoints` without changing the existing admin registration detail endpoint.
- [x] 2.5 Do not require an `Authorization` bearer token or email-verification token; rely on `X-Api-Key` plus the registration ID bearer-link model.

## 3. Tests

- [x] 3.1 Add handler or integration tests covering successful reduced detail mapping, current ticket selection, and empty additionalDetails.
- [x] 3.2 Add API tests covering missing API key returns 401 and a valid key for another team cannot read the registration.
- [x] 3.3 Add API tests covering unknown registration ID and registration from another event returning not found.
- [x] 3.4 Add an API response-shape assertion that admin-only fields are not present in the Partner payload.
- [x] 3.5 Add API tests proving the endpoint does not require an `Authorization` bearer token when `X-Api-Key`, event slug, and registration ID are valid.

## 4. Verification

- [x] 4.1 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj` and fix any architecture violations first.
- [x] 4.2 Run the targeted Registrations integration/API test suites changed by this work.
