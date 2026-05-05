## Context

Email templates are stored per scope (team or event) in the Email module and resolved with precedence: event > team > built-in default. The backend already exposes CRUD endpoints for stored (custom) templates. However, there is no way to preview the *resolved* template output before it reaches attendees, no Admin UI to browse or manage templates by type, and no test-send tied to the effective template (only the SMTP-connectivity test tied to email settings exists).

The Admin UI currently has email settings pages at `/teams/{teamSlug}/settings/email` and the event equivalent, but no template management pages.

## Goals / Non-Goals

**Goals:**
- Add backend endpoints to preview the resolved (effective) template for a given type using sample placeholder data.
- Add a backend endpoint to send a rendered test email for a specific template type to a chosen recipient.
- Add Admin UI pages for listing and editing email templates at both team and event scope.
- Expose template preview inline on the template detail page.
- Include send-test-email on the template detail page, with the recipient chosen from team-member email addresses.

**Non-Goals:**
- Live preview with actual event data (always uses placeholder/sample variables).
- Editing built-in default templates in-place (organizers may only add a custom override).
- Bulk testing or scheduling test sends.
- Template syntax validation on the frontend (backend validates on upsert).

## Decisions

### Decision: Preview uses placeholder sample data, not real event context

**Rationale**: The preview endpoint is called from the settings page before any event context is necessarily selected, and it must work for team-scoped templates that are not tied to any specific event. Hard-coded placeholder values (e.g. `event_name = "DevConf 2026"`, `first_name = "Alice"`) keep the endpoint simple and stateless.

*Alternative considered*: Accept an optional event slug and use that event's real data. Rejected because it adds complexity and the preview goal is merely to confirm layout and variable substitution.

### Decision: Preview endpoint is a GET request

**Rationale**: No state is mutated; the URL naturally encodes the template type and scope through the existing path parameters. GETs are easier to call from the browser and can be cached if desired.

*Alternative considered*: POST with a body carrying the current editor content so the organizer can preview unsaved edits. Deferred to a future iteration — the immediate need is to preview the *saved/resolved* template.

### Decision: Test-send for templates lives in a separate endpoint from the existing settings test-send

**Rationale**: The settings test-send (`POST /…/email-settings/test`) verifies SMTP connectivity by sending a plain-text probe message. The new template test-send (`POST /…/email-templates/{type}/test-send`) verifies template correctness by rendering the effective template and dispatching it through the resolved email settings. They have distinct purposes.

### Decision: Recipient pool for test-send is team member emails plus the configured SMTP from-address

**Rationale**: Team members (organizers) are the primary audience for test emails. Including the scope's configured `fromAddress` is useful because it lets the organizer verify the email arrives correctly from the sending address without having to be a registered team member under that email. The from-address is already loaded on the same page (email settings). Attendee emails require an active event context and add cross-module coupling for limited benefit.

### Decision: Admin UI template list replaces ad-hoc "go to template" links

The email settings page adds a "Templates" subsection linking to a list page at `…/settings/email/templates`. Each template type is a row with a "Custom" badge if a stored template exists, falling back to "Default". Clicking a row opens the template detail page.

### Decision: Template detail page reuses the email-settings page pattern (separate route, shared component)

Template detail page lives at `…/settings/email/templates/{type}`. The form (subject, text body, HTML body) is wired to the existing upsert/delete endpoints. A "Preview" panel beneath the form renders the GET preview response in an iframe or styled block. A "Send test email" button opens a dialog to pick a recipient.

## Risks / Trade-offs

- **Placeholder data may not reflect real rendering**: If a template uses variables not covered by the sample set, preview output may contain literal `{{ variable }}` tokens. Mitigation: document the sample variable set; Scriban silently ignores unknown variables in lenient mode, so rendering won't fail.
- **GET preview returns HTML body**: Response body includes raw HTML. The Admin UI must sanitise or sandbox (e.g. iframe sandbox) before displaying to avoid XSS. Mitigation: use an `<iframe srcdoc>` with no external scripts allowed.
- **Template list requires knowing all supported types up-front**: The UI hard-codes the list of supported type names (ticket, cancellation, reconfirm, etc.) since there is no "list all types" endpoint. Mitigation: the supported types are stable and defined in the spec; adding a type also requires backend work.
