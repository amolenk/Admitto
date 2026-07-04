## 1. SMTP Message Construction

- [x] 1.1 Update `MailKitEmailSender` to use the team reply-to address as the MIME `From` display name when present while preserving the configured system `From` address.
- [x] 1.2 Update `MailKitBulkSmtpSender` to apply the same MIME `From` display-name rule for bulk email sessions.
- [x] 1.3 Keep existing `Reply-To` header behavior unchanged for both send paths.

## 2. Tests

- [x] 2.1 Add or update tests for transactional SMTP message construction with a reply-to address, asserting display name, actual `From` address, and `Reply-To` header.
- [x] 2.2 Add or update tests for transactional SMTP message construction without a reply-to address, asserting the system sender address remains the visible display name.
- [x] 2.3 Add or update tests that cover the bulk SMTP send path using the same display-name behavior.

## 3. Documentation And Verification

- [x] 3.1 Update architecture documentation if implementation details alter documented email sender identity, reply routing, or runtime send behavior.
- [x] 3.2 Run architecture tests: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 3.3 Run targeted email tests for the changed Email module behavior.
