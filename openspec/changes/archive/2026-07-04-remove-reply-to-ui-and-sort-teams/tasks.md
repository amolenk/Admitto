## 1. Backend: alphabetical team ordering

- [x] 1.1 Add case-insensitive `OrderBy` on team name to the admin branch of `src/Admitto.Core/Organization/Application/UseCases/Teams/GetTeams/GetTeamsHandler.cs`
- [x] 1.2 Add the same ordering to the member ("my teams") branch of `GetTeamsHandler.cs`
- [x] 1.3 Add/extend handler tests covering alphabetical ordering (mixed-case team names) for both branches, using existing fixture/builder patterns

## 2. Admin UI: remove reply-to field from Team Settings

- [x] 2.1 Remove `replyToEmailAddress` from the Zod schema and default form values in `src/Admitto.UI.Admin/app/(dashboard)/teams/[teamId]/settings/team-settings-form.tsx`
- [x] 2.2 Remove the submit-time `replyToEmailAddress` / `clearReplyToEmailAddress` body assignments so the update request never includes reply-to fields
- [x] 2.3 Remove the "Reply-to email" `<FormField>` markup from the settings form
- [x] 2.4 Verify no other hand-written Admin UI code references reply-to (generated SDK types and `openapi-spec.json` are expected to keep it)

## 3. Verification

- [x] 3.1 Run architecture tests: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`
- [x] 3.2 Run Organization module test suite for the GetTeams changes
- [x] 3.3 Build/lint the Admin UI (`pnpm` build or lint in `src/Admitto.UI.Admin`) and manually verify the settings form has no reply-to field and the team switcher lists teams alphabetically
