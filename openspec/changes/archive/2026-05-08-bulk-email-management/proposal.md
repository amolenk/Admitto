## Why

Organizers need to communicate with attendees and potential registrants beyond automated transactional emails. Today there is no Admin UI to see the history of all bulk email activity for an event, and no way to create reusable custom email templates for ad-hoc campaigns (e.g. an alumni invitation for the next edition). The existing `bulk-email` backend already supports the send mechanics; what is missing is the UI and a reusable template store.

## What Changes

- The **"Emails" sidebar link** under an event currently navigates to the event email settings page (`/settings/email`). This link SHALL instead navigate to a new **Bulk Emails list page** at `/teams/{teamSlug}/events/{eventSlug}/emails`.
- A new **Bulk Emails list page** shows all bulk email jobs for the event (system-triggered and organizer-initiated) in a table, similar in style to the Registrations page.
- A new **Send Bulk Email flow** lets organizers select a previously created custom template and choose a recipient source: registered attendees (with optional filters) or an **external list via CSV upload**.
- A new **Custom Bulk Email Templates** section is added to the event (and team) email settings. Organizers create named templates (name, subject, text body, HTML body) here. These are distinct from the five system template types (`ticket`, `reconfirm`, etc.) and are stored as a new backend entity.
- The navigation active-state logic for the sidebar "Emails" entry is updated so that Settings → Email sub-pages no longer trigger the Emails item.

## Capabilities

### New Capabilities

- `admin-ui-bulk-emails`: Admin UI pages for bulk email management. Covers the list page, job detail page, and send dialog (template selection + recipient source with attendee filters or CSV upload), plus proxy API routes to the existing backend bulk-email endpoints.
- `custom-bulk-templates`: Backend CRUD for named custom bulk email templates stored per event (or team) scope. Provides admin endpoints for create, list, get, update, and delete. These templates supply the ad-hoc `subject`/`textBody`/`htmlBody` content that the existing bulk-email job `POST` endpoint already accepts.

### Modified Capabilities

- `admin-ui-email-templates`: A new "Custom templates" section is added to the event (and team) email settings templates area, allowing organizers to create, edit, and delete named custom bulk email templates.
- `admin-ui-event-management`: The sidebar "Emails" navigation entry is redefined to link to the new bulk-emails list page (`/emails`) instead of `/settings/email`. Active-state matching logic for the settings sub-navigation is updated accordingly.

## Impact

- **Admin UI** (`src/Admitto.UI.Admin`): New pages and proxy routes under `/teams/[teamSlug]/events/[eventSlug]/emails` and `/api/…/custom-bulk-templates`. Changes to `nav-event-pages.tsx` and the email templates settings area.
- **Backend** (`src/Admitto.Module.Email`): New `CustomBulkTemplate` aggregate (or value-object collection) with admin CRUD endpoints. The existing `POST /admin/…/bulk-emails` endpoint is unchanged; the UI reads the selected template and passes its content as the ad-hoc fields.
- **No breaking changes** to existing email settings or templates pages.
