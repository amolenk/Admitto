## 1. Fix SelfCancelRegistrationHttpEndpoint

- [x] 1.1 Remove `IVerificationTokenService` parameter from `HandleAsync` in `SelfCancelRegistrationHttpEndpoint`
- [x] 1.2 Remove `HttpRequest httpRequest` parameter from `HandleAsync`
- [x] 1.3 Delete the `ExtractBearerToken` helper method
- [x] 1.4 Delete the bearer-token extraction and `verificationTokenService.Validate` call (the two `if` blocks returning `Results.Unauthorized()`)
- [x] 1.5 Remove unused `using` directives left behind by the deletion

## 2. Fix SelfChangeTicketsHttpEndpoint

- [x] 2.1 Remove `IVerificationTokenService` parameter from `HandleAsync` in `SelfChangeTicketsHttpEndpoint`
- [x] 2.2 Remove `HttpRequest httpRequest` parameter from `HandleAsync`
- [x] 2.3 Delete the `ExtractBearerToken` helper method
- [x] 2.4 Delete the bearer-token extraction and `verificationTokenService.Validate` call
- [x] 2.5 Remove unused `using` directives left behind by the deletion

## 3. Tests

- [x] 3.1 Find existing tests for `SelfCancelRegistrationHttpEndpoint` and remove any bearer-token setup; verify the test still passes (cancellation succeeds without a token)
- [x] 3.2 Find existing tests for `SelfChangeTicketsHttpEndpoint` and remove any bearer-token setup; verify the test still passes (ticket change succeeds without a token)
- [x] 3.3 Confirm no new test scenarios are required (all spec scenarios are already covered by existing tests, adjusted for no-token)

## 4. Build & Verify

- [x] 4.1 Build the solution and confirm no compile errors
- [x] 4.2 Run the Registrations module tests and confirm all pass
