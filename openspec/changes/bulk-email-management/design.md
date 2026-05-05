## Context

The Admin UI currently has no screen dedicated to bulk email management for an event. The "Emails" sidebar entry navigates directly to the event email settings page (SMTP configuration). The `bulk-email` backend spec already defines all necessary send endpoints (`POST /`, `GET /`, `GET /{id}`, `POST /preview`, `POST /{id}/cancel`), so the send flow is a pure UI concern.

The backend `email-templates` spec explicitly reserves `bulk-custom` as a type that **cannot** be stored as a regular `EmailTemplate`. For reusable custom templates (Option B), a new `CustomBulkTemplate` backend entity is needed. The admin UI reads a selected template's content and passes it as the ad-hoc `subject`/`textBody`/`htmlBody` fields to the existing `POST /admin/…/bulk-emails` endpoint — so the send endpoint itself requires no changes.

The Registrations page establishes the table-and-filter pattern for list pages. The email templates settings area (`admin-ui-email-templates`) establishes the template management pattern.

## Goals / Non-Goals

**Goals:**
- Introduce a Bulk Emails list page per event showing all bulk email jobs (system and organizer-initiated) in a status-filterable table.
- Provide a Send Bulk Email dialog where organizers select a custom template and a recipient source (attendee filters or CSV upload).
- Add a Custom Templates management section to the email settings area for creating/editing/deleting named custom bulk email templates.
- Redirect the "Emails" sidebar item to the new list page; keep email settings accessible via Settings.

**Non-Goals:**
- Inline (one-off) email composition directly in the send flow — organizers must create a template first.
- Scheduled/recurring custom bulk emails.
- Rich HTML editor (organizers provide raw HTML; a preview panel is available via the existing template detail page).
- Modifying the existing `POST /admin/…/bulk-emails` endpoint.

## Decisions

### Decision: Page URL is `/events/{eventSlug}/emails` (not under `/settings`)

Operational activity (compose, send, monitor) belongs alongside Registrations, not inside Settings. The settings area is for configuration.

### Decision: Custom templates are a new backend entity, not a new fixed `EmailTemplate` type

The backend `email-templates` spec forbids storing `bulk-custom` as a template. Named custom templates require their own aggregate/table (`custom_bulk_templates`) with CRUD endpoints, separate from the system template resolution pipeline.

### Decision: Send flow reads template content client-side and submits as ad-hoc fields

When the organizer picks a template in the send dialog, the UI fetches its content and populates the ad-hoc `subject`/`textBody`/`htmlBody` fields sent to `POST /admin/…/bulk-emails`. The backend job stores the content verbatim — no template-ID reference on the job. This keeps the bulk-email spec unchanged and gives a full content snapshot on the job for audit purposes.

Alternative considered: store a `customTemplateId` on the job and resolve at render time — rejected because it creates a dependency between two entities and complicates the audit trail if a template is later edited or deleted.

### Decision: CSV upload is parsed client-side and submitted as `externalList` payload

The backend `ExternalListSource` accepts `(email, displayName?)` pairs. Parsing in the browser avoids a file-upload endpoint. Cap CSV at 5,000 rows client-side.

### Decision: Send dialog is a multi-step dialog (not a separate page)

Consistent with the "Add registration" pattern. Two steps: (1) select template, (2) choose recipient source + preview count.

### Decision: Custom templates section added to existing templates settings area

The existing `admin-ui-email-templates` templates page already lists the system types. A new "Custom templates" tab or section is added there rather than creating a separate settings page, keeping all template management in one place.

## Risks / Trade-offs

- [Risk] Organizers must create a template before they can send — slight extra friction → Mitigation: the send dialog includes a shortcut link to create a template; the empty-state copy explains the workflow.
- [Risk] Large CSVs parsed client-side may cause UI lag → Mitigation: 5,000-row cap with a client-side validation error.
- [Risk] Template content is snapshotted on the job — editing the template after sending does not affect in-progress or past sends → This is a feature, not a risk; it ensures audit integrity.

## Migration Plan

1. Add `CustomBulkTemplate` backend entity and admin CRUD endpoints.
2. Create UI template management section and proxy routes.
3. Create bulk emails list, detail, and send dialog pages and proxy routes.
4. Update `nav-event-pages.tsx` and active-state logic.
5. No database migrations to existing tables; new `email.custom_bulk_templates` table added.
6. Rollback: revert sidebar href; the new pages and endpoints are additive.
