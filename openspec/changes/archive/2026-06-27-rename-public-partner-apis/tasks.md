## 1. API Boundary Naming

- [x] 1.1 Rename API-key-protected endpoint grouping from Public API to Partner API in `Admitto.Api` while preserving `/api/...` routes.
- [x] 1.2 Rename Registrations module mapping methods, namespaces, and OpenAPI tags for API-key-protected endpoints from public terminology to partner terminology where practical.
- [x] 1.3 Rename API test helpers and test descriptions from Public API to Partner API where they refer to API-key-protected `/api/...` endpoints.
- [x] 1.4 Verify existing Partner API endpoints still require `X-Api-Key` and still use `/api/events/{eventId}/...` routes.

## 2. Direct Public Event Links

- [x] 2.1 Rename the `ResolvePublicEventLink` use-case slice to `DirectPublicEventLinks`.
- [x] 2.2 Implement anonymous redirect route `GET /e/{eventSlug}` to the event website URL.
- [x] 2.3 Implement anonymous redirect route `GET /e/{eventSlug}/register` to the website-relative `register` path.
- [x] 2.4 Implement anonymous redirect route `GET /e/{eventSlug}/cancel/{registrationId:guid}` to the website-relative cancellation path.
- [x] 2.5 Implement anonymous redirect route `GET /e/{eventSlug}/edit/{registrationId:guid}` to the website-relative edit path.
- [x] 2.6 Ensure redirect URL construction preserves existing website URL path prefixes and ignores request-controlled redirect targets.

## 3. Public QR Code

- [x] 3.1 Move QR-code route mapping from the Partner API group to the anonymous Public API `/e` group.
- [x] 3.2 Add `GetQRCodeQuery` and `GetQRCodeHandler` for public-slug and registration-ID based lookup and PNG generation.
- [x] 3.3 Keep QR-code payload, PNG content type, filename, error-correction level, and cancelled-registration behavior consistent with the existing capability.
- [x] 3.4 Stop exposing `GET /api/events/{eventId}/registrations/{registrationId}/qr-code` as a Partner API endpoint.

## 4. Tests

- [x] 4.1 Add or update API tests for `/e/{eventSlug}`, `/e/{eventSlug}/register`, `/e/{eventSlug}/cancel/{registrationId}`, and `/e/{eventSlug}/edit/{registrationId}` redirects.
- [x] 4.2 Add API tests for unknown public slugs returning not found without redirecting.
- [x] 4.3 Add or update QR-code API tests for anonymous `GET /e/{eventSlug}/qr-code/{registrationId}` success, unknown event, unknown registration, and cancelled registration.
- [x] 4.4 Add a route regression test proving the old API-key-protected QR-code endpoint no longer serves QR-code images.
- [x] 4.5 Update impacted test fixture names and helper usage after Public API to Partner API renaming.

## 5. Documentation and Generated Artifacts

- [x] 5.1 Update `docs/arc42/08-crosscutting-concepts.md` to define Admin API, Partner API, and Public API trust boundaries.
- [x] 5.2 Update `docs/arc42/06-runtime-view.md` to rename the `/api/...` attendee/partner flow and document anonymous `/e/...` redirect and QR-code flows.
- [x] 5.3 Update `docs/arc42/07-deployment-view.md` with `tickets.admitto.org` as an Azure Container Apps custom domain/host alias, including direct CNAME and certificate binding guidance.
- [x] 5.4 Regenerate affected OpenAPI/Admin UI SDK artifacts if route names, tags, or schemas change.

## 6. Verification

- [x] 6.1 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 6.2 Run targeted Registrations API tests covering public links, QR codes, and Partner API authentication.
- [x] 6.3 Run any affected Admin UI SDK/type checks if OpenAPI generation changes Admin UI generated files.
