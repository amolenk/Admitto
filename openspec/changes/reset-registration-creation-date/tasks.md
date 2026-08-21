## 1. Registration Reset Behavior

- [ ] 1.1 Update the `Registration` aggregate reset operation to set `CreatedAt` to its supplied reset time while preserving the registration ID and all existing reset semantics.
- [ ] 1.2 Verify that the admin, self-service, and coupon handlers continue to supply their clock-derived registration time to the shared reset operation without channel-specific changes.

## 2. Regression Coverage

- [ ] 2.1 Add a domain test proving that resetting a cancelled registration sets `CreatedAt` to the reset time while retaining the original registration ID and emitting the corresponding reset-time event.
- [ ] 2.2 Extend the existing reset integration coverage as needed to verify each reset-capable channel preserves the shared timestamp behavior.
- [ ] 2.3 Run the Registrations domain tests that cover `Registration` lifecycle behavior.

## 3. Validation

- [ ] 3.1 Run `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [ ] 3.2 Run the targeted Registrations domain test project and resolve any failures.
