## Why

The current email model is too flexible for actual operational needs: event-scoped SMTP settings, event/team template overrides, and editable transactional copy create configuration, UI, migration, and support complexity disproportionate to the value they provide. Real test-environment use shows that a team-level sender configuration plus simple branding is sufficient, while custom bulk email still needs explicit organizer-authored content.

## What Changes

- **BREAKING** Remove event-scoped email settings. Email settings become team-scoped only, and all event-owned application emails use the owning team's SMTP configuration.
- **BREAKING** Remove persisted editable transactional email templates at both team and event scope. Transactional email content becomes code-owned built-in content.
- Add simple team-level email branding for built-in transactional emails: accent color and a font-family string selected from a minimal UI-configured set.
- Keep font choices as a UI concern. The API stores the selected font as a string and does not validate whether it is an actual or email-safe font.
- Keep custom bulk email support, but require each custom bulk send to carry explicit `Subject`, `TextBody`, and `HtmlBody` on the `BulkEmailJob`.
- Remove template-selection behavior from custom bulk email creation. Organizers author content directly for each bulk send instead of choosing a stored template.
- Update Admin UI surfaces to remove event email settings and template management, add team branding controls, and make bulk email creation collect subject, text body, and HTML body directly.
- Update architecture documentation to describe team-only settings, built-in themed transactional emails, and job-owned custom bulk content.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `email-settings`: email server settings become team-scoped only; effective event settings resolve directly from the owning team's row.
- `team-email-settings`: the old "team fallback for event settings" behavior is replaced by team-only settings and branding.
- `email-templates`: editable stored templates are removed; built-in transactional templates remain code-owned and are rendered with team branding.
- `email-sending`: transactional email composition uses built-in themed content rather than configurable templates.
- `bulk-email`: custom bulk jobs require explicit subject, text body, and HTML body, and no longer resolve or partially override stored templates.
- `custom-bulk-templates`: reusable custom bulk template CRUD is removed; custom bulk content is entered per send.
- `admin-ui-team-email-settings`: team email settings UI remains, but is the only SMTP configuration surface and gains simple branding controls.
- `admin-ui-email-templates`: template-management UI is removed because transactional templates are no longer editable.
- `admin-ui-bulk-emails`: custom bulk email creation collects direct subject/text/html content instead of selecting a template.

## Impact

- Email domain/persistence: simplify `EmailSettings` scope, remove or ignore `EmailTemplate` persistence, add team email branding storage.
- Email application layer: replace event/team fallback resolvers with team-only settings and built-in transactional template rendering with branding.
- Bulk email pipeline: preserve single SMTP session fan-out, recipient snapshots, idempotency, cancellation, and logging while changing content input rules.
- API: remove event-scoped settings endpoints and all template management/preview/test-send endpoints; adjust bulk email request validation/contracts.
- Admin UI: remove event email settings pages and template pages; regenerate and use the generated SDK after API changes; update bulk-send sheet/forms.
- Database: schema can be simplified destructively because production migration is not required yet; fresh deployment is acceptable.
- Documentation/specs: update OpenSpec specs and arc42 chapter 5/6/8 where email settings/templates/runtime composition are described.
