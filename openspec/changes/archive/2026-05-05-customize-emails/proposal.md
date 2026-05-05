## Why

Organizers currently have no way to preview, customize, or test the email templates that Admitto sends to attendees. Without this, teams must configure templates blind and rely on live sends to see the results — a poor experience that risks sending broken emails to real attendees.

## What Changes

- **ADD** preview endpoint: render a template with sample data and return the result so organizers can inspect output before publishing.
- **ADD** Admin UI template management page per scope (team and event): list all template types, show whether each is customized or using the built-in default, and allow the organizer to view/edit the current template.
- **ADD** upsert (add/overwrite) custom template: organizers can save a custom subject, text body, and HTML body for any template type at team or event scope.
- **ADD** delete custom template: revert a customized template back to the next-in-precedence template (team-scoped or built-in default).
- **ADD** send-test-email action on the template settings page: organizers pick one recipient from team-member email addresses or from recent attendee email addresses and fire a rendered test email to that address.

## Capabilities

### New Capabilities
- `admin-ui-email-templates`: Admin UI pages and proxy routes for listing, previewing, upserting, and deleting email templates at team or event scope, plus sending a test email.

### Modified Capabilities
- `email-templates`: Add a preview endpoint (render template with sample/placeholder data and return the rendered result) and a send-test-email endpoint (render and dispatch a single email to a chosen recipient).

## Impact

- **Backend (Email module)**: Two new admin endpoints — `GET /admin/…/email-templates/{type}/preview` and `POST /admin/…/email-templates/{type}/test-send`. Existing upsert/delete endpoints already exist per the `email-templates` spec.
- **Admin UI**: New template-list and template-detail pages under the existing team/event settings layouts; new Next.js proxy routes for all template endpoints.
- **No breaking changes** to existing API contracts.
