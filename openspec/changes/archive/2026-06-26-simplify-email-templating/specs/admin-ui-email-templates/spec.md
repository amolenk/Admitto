## REMOVED Requirements

### Requirement: Email settings page links to a template list sub-page
**Reason**: Transactional templates and custom bulk templates are no longer managed as reusable resources.
**Migration**: Remove links to template list pages from team and event email/settings pages.

#### Scenario: Template links removed
- **WHEN** an organizer opens team or event email pages
- **THEN** no Templates link or tab is shown

### Requirement: Template list page enumerates all supported types
**Reason**: Built-in transactional templates are code-owned and not listed for editing.
**Migration**: Remove team/event template list routes and redirect or delete old pages.

#### Scenario: Template list route removed
- **WHEN** a browser navigates to an old template list route
- **THEN** the route is no longer part of the supported Admin UI navigation

### Requirement: Template detail page loads the stored custom template or empty defaults
**Reason**: Stored transactional templates are removed.
**Migration**: Remove template detail pages and associated data fetching.

#### Scenario: Template detail page removed
- **WHEN** code search inspects Admin UI routes
- **THEN** no transactional template detail page remains

### Requirement: Template detail page saves via the upsert endpoint
**Reason**: There is no transactional template upsert endpoint.
**Migration**: Remove save forms and proxy calls for template upsert.

#### Scenario: Template save behavior removed
- **WHEN** an organizer uses the Admin UI
- **THEN** there is no form for saving transactional template subject/body content

### Requirement: Template detail page supports deleting the custom template
**Reason**: There are no stored transactional template overrides to delete.
**Migration**: Remove delete actions for transactional templates.

#### Scenario: Template delete behavior removed
- **WHEN** an organizer uses the Admin UI
- **THEN** there is no delete action for transactional template overrides

### Requirement: Template detail page shows a rendered preview
**Reason**: Preview was for editable templates. Built-in content is code-owned and covered by tests.
**Migration**: Remove preview panels and preview proxy calls.

#### Scenario: Template preview panel removed
- **WHEN** an organizer uses the Admin UI
- **THEN** no transactional template preview panel is shown

### Requirement: Template detail page supports sending a test email
**Reason**: Template-specific test sends are tied to editable templates. SMTP diagnostics remain available through team email settings.
**Migration**: Remove template test-send dialogs and proxy calls.

#### Scenario: Template test-send removed
- **WHEN** an organizer uses the Admin UI
- **THEN** no transactional template-specific test-send action is shown

### Requirement: Admin UI exposes Next.js proxy routes for template preview and test-send endpoints
**Reason**: Backend template preview and test-send endpoints are removed.
**Migration**: Remove the corresponding Next.js proxy routes and generated SDK callers.

#### Scenario: Template proxy routes removed
- **WHEN** code search inspects Admin UI API routes
- **THEN** no transactional template preview or test-send proxy routes remain

### Requirement: Email templates area includes a Custom Templates section for bulk email templates
**Reason**: Reusable custom bulk templates are removed. Custom bulk content is authored directly in the send flow.
**Migration**: Remove the Custom Templates section from email pages.

#### Scenario: Custom Templates section removed
- **WHEN** an organizer opens event email pages
- **THEN** no Custom Templates section is shown

### Requirement: Organizers can create a custom bulk email template from the templates page
**Reason**: Reusable custom bulk templates are removed.
**Migration**: Replace the create-template flow with direct subject/text/html fields in the Send bulk email sheet.

#### Scenario: Custom bulk template create flow removed
- **WHEN** an organizer wants to send a custom bulk email
- **THEN** they enter content in the send flow instead of creating a reusable template

### Requirement: Organizers can edit a custom bulk email template
**Reason**: Reusable custom bulk templates are removed.
**Migration**: Remove edit actions and pages/dialogs for custom bulk templates.

#### Scenario: Custom bulk template edit flow removed
- **WHEN** code search inspects the Admin UI
- **THEN** no custom bulk template edit form remains

### Requirement: Organizers can delete a custom bulk email template
**Reason**: Reusable custom bulk templates are removed.
**Migration**: Remove delete actions and proxy calls for custom bulk templates.

#### Scenario: Custom bulk template delete flow removed
- **WHEN** code search inspects the Admin UI
- **THEN** no custom bulk template delete action remains

### Requirement: Admin UI exposes proxy routes for custom-bulk-template endpoints
**Reason**: Backend custom-bulk-template endpoints are removed.
**Migration**: Remove custom-bulk-template Next.js proxy routes and generated SDK callers.

#### Scenario: Custom bulk template proxy routes removed
- **WHEN** code search inspects Admin UI API routes
- **THEN** no custom-bulk-template proxy routes remain
