## 1. Backend: Preview Endpoint

- [x] 1.1 Add `PreviewEmailTemplateQuery` record (scope, scopeId, type) and `PreviewEmailTemplateDto` (RenderedSubject, RenderedTextBody, RenderedHtmlBody)
- [x] 1.2 Implement `PreviewEmailTemplateHandler`: resolve the effective template via `EmailTemplateService.LoadAsync`, render it with sample placeholder data using `IEmailTemplateRenderer`, return the DTO
- [x] 1.3 Add `PreviewEmailTemplateHttpEndpoint` (`MapPreviewEmailTemplate`) with GET `/preview` mapped for both team and event scopes in `EmailApiEndpoints`
- [x] 1.4 Add sample placeholder values (e.g. `event_name`, `first_name`, `register_link`, `ticket_types`) used by preview and test-send rendering

## 2. Backend: Test-Send Endpoint for Templates

- [x] 2.1 Add `TestSendEmailTemplateCommand` record (scope, scopeId, type, recipient)
- [x] 2.2 Implement `TestSendEmailTemplateHandler`: resolve effective template, render with sample data, resolve email settings for scope, send via `IEmailSender`; return `email_settings.not_configured` error when no settings found
- [x] 2.3 Add `TestSendEmailTemplateHttpEndpoint` (`MapTestSendEmailTemplate`) with POST `/test-send` mapped for both team and event scopes; add request class and FluentValidation validator
- [x] 2.4 Wire preview and test-send routes into `EmailApiEndpoints.MapEmailAdminEndpoints`

## 3. Admin UI: Proxy Routes

- [x] 3.1 Add Next.js proxy route `app/api/teams/[teamSlug]/email-templates/[type]/preview/route.ts` forwarding `GET` to backend
- [x] 3.2 Add Next.js proxy route `app/api/teams/[teamSlug]/email-templates/[type]/test-send/route.ts` forwarding `POST` to backend
- [x] 3.3 Add event-scoped proxy routes for preview and test-send under `app/api/teams/[teamSlug]/events/[eventSlug]/email-templates/[type]/`
- [x] 3.4 Regenerate Admin UI SDK (`pnpm openapi-ts`) after new endpoints are live

## 4. Admin UI: Template List Page

- [x] 4.1 Create `app/(dashboard)/teams/[teamSlug]/settings/email/templates/page.tsx` — list all supported template types with Custom/Default badge; fetch custom template existence via existing GET endpoint (404 = Default)
- [x] 4.2 Create the event-scoped equivalent `app/(dashboard)/teams/[teamSlug]/events/[eventSlug]/settings/email/templates/page.tsx`
- [x] 4.3 Add "Templates" link to the team email settings page (`/teams/{teamSlug}/settings/email`)
- [x] 4.4 Add "Templates" link to the event email settings page

## 5. Admin UI: Template Detail Page

- [ ] 5.1 Create `app/(dashboard)/teams/[teamSlug]/settings/email/templates/[type]/page.tsx` with form (Subject, Text Body, HTML Body), wired to GET and PUT endpoints; show "Back to templates" link
- [ ] 5.2 Create the event-scoped equivalent template detail page
- [ ] 5.3 Implement delete action: show only when a custom template exists, prompt confirmation, call DELETE endpoint, clear form on success
- [ ] 5.4 Add "Preview" panel below the form: call the preview proxy route on page load, display rendered subject and HTML body (HTML in `<iframe srcdoc>` sandbox), add "Refresh preview" button

## 6. Admin UI: Send Test Email Dialog

- [ ] 6.1 Create `SendTestEmailDialog` component: dropdown of candidate recipients built from team member emails (fetched from members API) union the `fromAddress` from the loaded email settings for the current scope (deduplicated); confirm button, success/error notification
- [ ] 6.2 Wire the dialog into the template detail page (both team and event scopes); POST to the test-send proxy route with the selected recipient
- [ ] 6.3 Surface backend error message inside the dialog when test-send fails
