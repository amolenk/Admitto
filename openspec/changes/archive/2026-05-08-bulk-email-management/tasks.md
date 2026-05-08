## 1. Backend: Custom Bulk Templates

- [x] 1.1 Create `CustomBulkTemplate` aggregate in `Admitto.Module.Email` with fields: Id, TeamId, TicketedEventId (nullable), Name, Subject, TextBody, HtmlBody, CreatedAt, UpdatedAt, Version
- [x] 1.2 Add EF Core mapping and migration for `email.custom_bulk_templates` table with unique index on (TeamId, TicketedEventId, Name) case-insensitive
- [x] 1.3 Implement `CreateCustomBulkTemplateCommand` handler (validates unique name, returns new id)
- [x] 1.4 Implement `UpdateCustomBulkTemplateCommand` handler (optimistic concurrency via Version)
- [x] 1.5 Implement `DeleteCustomBulkTemplateCommand` handler
- [x] 1.6 Implement `GetCustomBulkTemplatesQuery` handler (list by event scope, ordered by name)
- [x] 1.7 Implement `GetCustomBulkTemplateQuery` handler (by id, with auth check)
- [x] 1.8 Wire admin endpoints under `/admin/teams/{teamSlug}/events/{eventSlug}/custom-bulk-templates` (GET list, POST, GET /{id}, PUT /{id}, DELETE /{id})
- [x] 1.9 Add FluentValidation validators for create/update requests (name required, subject required, textBody required)

## 2. Proxy Routes: Custom Bulk Templates

- [x] 2.1 Create `GET /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates` proxy route
- [x] 2.2 Create `POST /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates` proxy route
- [x] 2.3 Create `GET /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates/[id]` proxy route
- [x] 2.4 Create `PUT /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates/[id]` proxy route
- [x] 2.5 Create `DELETE /api/teams/[teamSlug]/events/[eventSlug]/custom-bulk-templates/[id]` proxy route

## 3. Proxy Routes: Bulk Emails

- [x] 3.1 Create `GET /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails` proxy route
- [x] 3.2 Create `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails` proxy route
- [x] 3.3 Create `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/preview` proxy route
- [x] 3.4 Create `GET /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/[jobId]` proxy route
- [x] 3.5 Create `POST /api/teams/[teamSlug]/events/[eventSlug]/bulk-emails/[jobId]/cancel` proxy route

## 4. Admin UI: Custom Templates Section in Email Settings

- [x] 4.1 Add "Custom Templates" section to the event email templates page (`settings/email/templates/page.tsx`)
- [x] 4.2 Fetch and render the custom templates table (Name, Subject preview, Edit/Delete actions)
- [x] 4.3 Implement empty state with "Create custom template" button
- [x] 4.4 Implement create dialog (Name, Subject, Text Body, HTML Body) wired to the POST proxy
- [x] 4.5 Implement edit dialog (pre-filled, uses PUT proxy with Version for optimistic concurrency)
- [x] 4.6 Implement delete confirmation and DELETE proxy call

## 5. Admin UI: Bulk Emails List Page

- [x] 5.1 Create page at `app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/emails/page.tsx` with breadcrumbs and PageLayout
- [x] 5.2 Fetch data via `GET /api/…/bulk-emails` and render the jobs table (Type, Status badge, Recipients, Sent, Failed, Triggered by, Created)
- [x] 5.3 Add status filter control (All / Active / Completed / Failed & Cancelled)
- [x] 5.4 Implement empty state with feature description, "Send bulk email" button, and "Configure email settings" link
- [x] 5.5 Make rows clickable and navigate to the detail page

## 6. Admin UI: Bulk Email Job Detail Page

- [x] 6.1 Create page at `app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/emails/[jobId]/page.tsx`
- [x] 6.2 Fetch job details from `GET /api/…/bulk-emails/[jobId]` and display summary (status, type, trigger, source, ad-hoc content, timestamps, totals)
- [x] 6.3 Show "Cancel" button for active jobs (calls cancel proxy, refreshes on success)
- [x] 6.4 Add "Back to bulk emails" link

## 7. Admin UI: Send Bulk Email Dialog

- [x] 7.1 Create `SendBulkEmailDialog` component with two-step structure
- [x] 7.2 Step 1 — Select template: fetch custom templates list, show dropdown, include "Create template" shortcut link; disable Send when no templates exist
- [x] 7.3 Step 2 — Recipients: toggle between "Registered attendees" and "External list (CSV)"
- [x] 7.4 Attendee source: ticket-type multi-select and registration-status filter; "Preview" button calls preview proxy and shows matched count + sample
- [x] 7.5 CSV source: file upload, client-side parse (email + optional name), preview table, 5,000-row cap with inline error
- [x] 7.6 Show send confirmation summary with selected template name and recipient count
- [x] 7.7 Wire "Send" button: POST with `emailType: "bulk-custom"`, selected template content as ad-hoc fields, and resolved source payload
- [x] 7.8 On success: close dialog, show "Bulk email queued" toast, refetch list
- [x] 7.9 Surface backend validation errors inline

## 8. Sidebar Navigation Update

- [x] 8.1 Update `nav-event-pages.tsx`: change "Emails" `href` from `/settings/email` to `/emails`
- [x] 8.2 Update `isPageActive` logic so the "Emails" entry is active only for `/emails` and `/emails/*` paths
