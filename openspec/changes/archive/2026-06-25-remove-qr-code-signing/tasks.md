## 1. QR-Code Contract

- [x] 1.1 Update `GetQRCodeHttpEndpoint` to remove the `signature` parameter and signature validation.
- [x] 1.2 Change QR-code generation to encode only the registration ID string.
- [x] 1.3 Preserve API-key authentication, event/team scoping, registration lookup, PNG response headers, and cancelled-registration behavior.

## 2. Link Generation And Signing Cleanup

- [x] 2.1 Update registration email context link construction so QR-code links do not include signatures.
- [x] 2.2 Search production code for `RegistrationSigner`, `IEventSigningKeyProvider`, and `TicketedEvent.SigningKey` usages and remove QR-only dependencies.
- [x] 2.3 If no production flow still uses the per-event signing key, remove signing services/model properties and generate the EF migration with the official EF tooling.
- [x] 2.4 Preserve unrelated HMAC mechanisms, specifically short-lived email verification tokens.

## 3. API Contract And Clients

- [x] 3.1 Regenerate OpenAPI/Admin UI SDK after the backend QR-code contract changes.
- [x] 3.2 Update any generated-client call sites or route helpers that still provide a QR-code signature.
- [x] 3.3 Confirm no handwritten API client/proxy replacement is introduced for the QR-code endpoint.

## 4. Tests

- [x] 4.1 Update QR-code API tests so successful retrieval does not provide a signature and expects a registration-ID-only payload.
- [x] 4.2 Remove or replace invalid/missing/cross-event signature tests with unknown-event and unknown-registration coverage matching the new spec.
- [x] 4.3 Keep coverage proving the endpoint requires `X-Api-Key` and cancelled registrations still return a PNG.
- [x] 4.4 Update or remove signing-service tests only for QR-specific signing code that no longer exists.

## 5. Documentation And Verification

- [x] 5.1 Update `docs/arc42/08-crosscutting-concepts.md` to replace per-event registration URL signing with registration-ID bearer-secret semantics for QR/self-service links.
- [x] 5.2 Update any other docs/spec references that still describe QR-code signatures or `registrationId:signature` payloads.
- [x] 5.3 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 5.4 Run targeted API tests for QR-code and self-service cancellation behavior.
- [x] 5.5 Run any targeted integration/domain tests affected by signing-key model or migration cleanup.
