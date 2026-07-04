## Context

The Email module currently treats SMTP settings and templates as scoped resources with event-level overrides over team-level defaults. The Worker resolves effective settings and templates during transactional sends and bulk fan-out. The Admin UI exposes separate team/event settings and template-management flows, and custom bulk email can be driven from stored custom templates.

In practice this flexibility is not needed. Organizers need one sender configuration per team, consistent branded transactional emails, and the ability to send one-off custom bulk emails with explicitly authored content. The current model creates avoidable fallback logic, UI surfaces, validation cases, migrations, and support scenarios for malformed organizer-edited transactional templates.

The existing architecture keeps Email concerns inside the Email module, exposes cross-module checks through `IEventEmailFacade`, and requires API endpoints to own unit-of-work commits. This change keeps those boundaries but changes the Email module's model and public API surface.

## Goals / Non-Goals

**Goals:**

- Make SMTP settings team-scoped only.
- Remove organizer-editable transactional templates at all scopes.
- Render transactional emails from code-owned built-in content using team-level branding.
- Provide only simple branding controls: accent color and a font-family value selected from a minimal UI-configured list.
- Require custom bulk sends to provide `Subject`, `TextBody`, and `HtmlBody` directly.
- Preserve existing email delivery reliability properties: outbox, `EmailLog` claims, retry behavior, bulk recipient snapshots, cancellation, and single SMTP connection fan-out.
- Remove Admin UI settings/templates surfaces that are no longer meaningful.

**Non-Goals:**

- No per-event sender settings.
- No team-level editable transactional copy.
- No event-level editable transactional copy.
- No reusable custom bulk template library.
- No separate external font URL, custom CSS, or Google Fonts URL feature.
- No redesign of the email delivery/idempotency pipeline beyond content/settings resolution.

## Decisions

1. Team-only SMTP settings replace effective event settings.

   `EmailSettings` should no longer carry `TicketedEventId`. The send path still receives `TeamId` and `TicketedEventId` because logs, idempotency, and event-scoped recipients still need event identity, but SMTP lookup uses only the team row. `IEventEmailFacade` can retain its event-oriented contract for Registrations while internally checking the owning team's settings.

   Alternative considered: keep nullable `TicketedEventId` and hide event endpoints. Rejected because it preserves the old mental model, indexes, stale data shape, and event override edge cases.

2. Branding belongs with team email configuration.

   Add team email branding fields alongside the team email configuration, using safe defaults when no explicit branding exists. The minimum model is `AccentColor` and `FontFamily`. `FontFamily` is stored by the API as a string. The Admin UI presents a minimal configured list of useful choices, for example `System`, `Arial`, `Georgia`, `Roboto`, and `Inter`, and submits the selected value. The backend does not need to validate that the string is an actual or email-safe font beyond normal string hygiene such as length.

   Alternative considered: backend enum/preset validation. Rejected because the list is a presentation concern and keeping it in the UI avoids backend churn when the available choices change.

3. Transactional templates become code-owned themed templates.

   The configurable `EmailTemplate` aggregate and its admin endpoints should be removed. Built-in templates remain in code/resources and are selected by email type. They receive the same event/registration parameters they need today, plus branding parameters generated from the owning team's branding settings. Because organizers cannot edit this content, deterministic render failures become code defects covered by tests rather than user-caused runtime configuration failures.

   Alternative considered: keep team-level editable transactional templates only. Rejected because the user explicitly does not need team-level editable transactional copy and because malformed templates remain a support burden.

4. Custom bulk content is job-owned and complete.

   A custom bulk send must include `Subject`, `TextBody`, and `HtmlBody`. The `BulkEmailJob` stores those values as the immutable content snapshot for that send. The bulk worker renders those job-owned fields for each recipient and no longer resolves a stored `EmailTemplate` or partially falls back to template fields. Reconfirm/system bulk jobs continue to use built-in code-owned content for their email type.

   Alternative considered: allow text-only or HTML-only custom bulk sends. Rejected because requiring both bodies avoids delivery-format ambiguity and keeps the send contract simple.

5. Remove reusable custom bulk templates.

   The `CustomBulkTemplate` capability and Admin UI management surface should be removed rather than migrated to a smaller feature. The use case is now direct per-send authoring.

   Alternative considered: retain custom bulk templates but make them team-scoped only. Rejected because it keeps a template library and selection workflow the user wants to avoid.

6. Documentation must change with the architecture.

   arc42 chapter 5 should describe team-only email settings, code-owned themed transactional templates, and job-owned custom bulk content. Runtime view diagrams should remove event/template fallback language. Cross-cutting concepts should continue to describe `EmailLog` idempotency but no longer reference user-edited transactional template failures as normal configuration errors.

## Risks / Trade-offs

- [Risk] Existing event-scoped settings/templates or custom bulk templates become obsolete. -> Accepted because production is not running yet and a fresh deployment is acceptable.
- [Risk] Teams that relied on per-event sender identities lose flexibility. -> Accepted trade-off; the desired product direction is team-level sender configuration only.
- [Risk] Built-in transactional copy changes now require deployments. -> Accepted trade-off; this improves consistency and eliminates admin-caused template breakage.
- [Risk] Font choices may not render identically across email clients. -> Treat the configured font string as best-effort styling and keep built-in templates readable with normal fallback behavior.
- [Risk] Removing API endpoints breaks generated Admin UI SDK and callers. -> Regenerate SDK after backend API changes and remove UI/proxy callers in the same change.
- [Risk] Bulk HTML supplied by admins can be malformed. -> Validate required presence and keep render/send failure behavior logged per recipient/job; sanitization is not introduced in this change.

## Migration Plan

1. Add/adjust Email module model for team-only settings plus branding defaults.
2. Generate an EF migration using official tooling that removes `ticketed_event_id` from `email_settings`, drops `email_templates` and custom bulk template storage, and adds branding fields/defaults. The migration does not need to preserve existing event/template data.
3. Remove event settings/template/custom-bulk-template endpoints and wire only team settings/branding and bulk-email endpoints.
4. Update send and bulk fan-out paths to use team-only settings and code-owned content.
5. Regenerate the Admin UI SDK via the Aspire-backed workflow before updating proxy/UI code.
6. Update Admin UI routes and remove dead pages/components.
7. Update arc42 and OpenSpec main specs when the change is archived.

Rollback can be destructive in local/test environments. Production data-preserving rollback is not a design goal for this change.

## Open Questions

- Which exact initial font choices should ship in the UI configuration? A minimal set of `System`, `Arial`, `Georgia`, `Roboto`, and `Inter` is sufficient unless design preferences say otherwise.
