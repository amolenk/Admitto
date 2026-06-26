## 1. Backend Model And Persistence

- [x] 1.1 Simplify `EmailSettings` to team scope only by removing event-scope state and resolver precedence logic.
- [x] 1.2 Add team email branding fields for accent color and font-family string with safe defaults and basic string hygiene.
- [x] 1.3 Remove `EmailTemplate` persistence, write-store access, template CRUD slices, preview slices, test-send slices, and related endpoint registrations.
- [x] 1.4 Remove `CustomBulkTemplate` persistence, endpoints, validators, write-store access, and related uniqueness/error handling.
- [x] 1.5 Generate an EF Core migration using the official EF workflow that destructively removes obsolete template/event-scope storage and adds branding fields; no production data preservation is required.
- [x] 1.6 Update Email module PostgreSQL exception mappings and indexes for team-only settings.

## 2. Backend Email Composition

- [x] 2.1 Replace effective settings resolution with team-only settings resolution while keeping event IDs for logs and idempotency.
- [x] 2.2 Update `IEventEmailFacade` implementation so event configuration checks use only the owning team's settings.
- [x] 2.3 Replace transactional template lookup with built-in code-owned content rendered with team branding.
- [x] 2.4 Render the configured font-family string in built-in transactional HTML without backend font-safety validation.
- [x] 2.5 Update registration, cancellation, ticket-change, OTP, waitlist, reconfirm, and auto-cancel email paths to use built-in themed content.
- [x] 2.6 Preserve deterministic render-failure handling for code-owned content and custom bulk job content.

## 3. Bulk Email Changes

- [x] 3.1 Change custom bulk email create requests and validators to require `subject`, `textBody`, and `htmlBody`.
- [x] 3.2 Update `BulkEmailJob` creation so `bulk-custom` jobs always persist complete job-owned content.
- [x] 3.3 Remove template-name/template-selection behavior from bulk send creation and fan-out.
- [x] 3.4 Update bulk fan-out rendering to use job-owned content for `bulk-custom` and built-in content for system bulk types.
- [x] 3.5 Keep existing recipient snapshot, cancellation, single SMTP connection, retry, and `EmailLog` idempotency behavior intact.

## 4. API And SDK

- [x] 4.1 Remove event-scoped email settings routes from backend endpoint registration.
- [x] 4.2 Remove email template and custom-bulk-template routes from backend endpoint registration.
- [x] 4.3 Update request/response DTOs for team email settings to include branding fields.
- [x] 4.4 Update request/response DTOs for bulk email creation to require direct custom content.
- [x] 4.5 Regenerate the Admin UI SDK through the approved Aspire-backed OpenAPI workflow before changing proxy/UI callers.

## 5. Admin UI

- [x] 5.1 Remove event-scoped email settings pages, forms, proxy routes, queries, links, and navigation entries.
- [x] 5.2 Remove transactional template and custom bulk template pages, forms, proxy routes, queries, links, and navigation entries.
- [x] 5.3 Update the team email settings page to show SMTP settings plus accent color and a font selector backed by a minimal UI-configured option list.
- [x] 5.4 Update team email settings proxy code to use regenerated SDK functions and include branding fields.
- [x] 5.5 Update the bulk email Sheet to collect subject, text body, HTML body, and recipients directly.
- [x] 5.6 Remove bulk email template-loading code and ensure create requests send direct content to the generated SDK/proxy route.
- [x] 5.7 Verify desktop and mobile layout for the simplified team email settings page and bulk email Sheet.

## 6. Tests

- [x] 6.1 Update or remove domain tests for event-scoped settings, email templates, and custom bulk templates.
- [x] 6.2 Add domain/application tests for team-only settings, branding defaults, and API string storage for font-family values.
- [x] 6.3 Update email sending integration tests to assert built-in themed content and team-only settings resolution.
- [x] 6.4 Update bulk email integration tests to assert required subject/text/html and no template fallback.
- [x] 6.5 Update API tests to cover removed routes, changed DTO validation, and retained team settings/test-send behavior.
- [x] 6.6 Update Admin UI tests if present for removed template/settings surfaces and the direct-content bulk send flow.

## 7. Documentation And Verification

- [x] 7.1 Update `docs/arc42/05-building-block-view.md` to describe team-only settings, team branding, built-in templates, and job-owned custom bulk content.
- [x] 7.2 Update `docs/arc42/06-runtime-view.md` to remove event/template fallback from send and bulk-email flows.
- [x] 7.3 Update `docs/arc42/08-crosscutting-concepts.md` if render-failure or secret-protection wording references event-scoped settings/templates.
- [x] 7.4 Run architecture tests first: `dotnet test --project tests/Admitto.Core.ArchTests/Admitto.Core.ArchTests.csproj`.
- [x] 7.5 Run targeted Email module domain/integration/API tests after architecture tests pass.
- [x] 7.6 Run targeted Admin UI lint/type/test checks for changed UI and generated SDK code.
