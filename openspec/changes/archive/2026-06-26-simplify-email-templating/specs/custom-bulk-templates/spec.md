## REMOVED Requirements

### Requirement: Custom bulk email templates are first-class entities with CRUD support
**Reason**: Custom bulk email content is now entered directly per send and stored on `BulkEmailJob`. A reusable custom template library is no longer part of the product.
**Migration**: Remove `CustomBulkTemplate` persistence, endpoints, generated SDK functions, proxy routes, and Admin UI management surfaces. Existing custom bulk template records are not migrated; organizers provide subject, text body, and HTML body when creating a new custom bulk send.

#### Scenario: Custom bulk template CRUD removed
- **WHEN** the API contract is generated
- **THEN** custom-bulk-template list/create/get/update/delete endpoints are absent

### Requirement: Custom bulk template names are unique within their scope
**Reason**: Custom bulk template records no longer exist, so template-name uniqueness no longer applies.
**Migration**: Drop related uniqueness constraints with the custom bulk template storage.

#### Scenario: Custom bulk template uniqueness removed
- **WHEN** the database schema is migrated
- **THEN** no custom-bulk-template name uniqueness constraint remains
