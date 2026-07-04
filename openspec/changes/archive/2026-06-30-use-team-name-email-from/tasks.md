## 1. Effective Sender Settings

- [x] 1.1 Extend `EffectiveEmailSettings` with an explicit `FromDisplayName` value separate from `FromAddress` and `ReplyToAddress`.
- [x] 1.2 Update `EffectiveEmailSettingsResolver` to read `TeamName` and `ReplyToEmailAddress` from `IEmailReadStore.TeamEmailContexts` in one projection query.
- [x] 1.3 Resolve `FromDisplayName` to projected team name when available, otherwise the configured Admitto sender address.

## 2. SMTP Message Construction

- [x] 2.1 Update `MailKitMimeMessageBuilder` to use `EffectiveEmailSettings.FromDisplayName` for the MIME `From` display name.
- [x] 2.2 Update the bulk SMTP session path to pass the resolved display name through to `MailKitMimeMessageBuilder`.
- [x] 2.3 Keep `Reply-To` header behavior unchanged and independent from the `From` display name.

## 3. Tests

- [x] 3.1 Update `EffectiveEmailSettingsResolverTests` to assert projected team name becomes the sender display name and missing team name falls back to the system sender address.
- [x] 3.2 Update `MailKitMimeMessageBuilderTests` to assert reply-to no longer drives the `From` display name.
- [x] 3.3 Cover the bulk send construction path with team-name display behavior.
- [x] 3.4 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 3.5 Run targeted Email integration tests for settings resolution and MIME message construction.

## 4. Documentation

- [x] 4.1 Update `docs/arc42/05-building-block-view.md` to describe team name as the sender label and reply-to as reply routing only.
- [x] 4.2 Update `docs/arc42/08-crosscutting-concepts.md` Email context projection notes to match the new sender display-name behavior.
