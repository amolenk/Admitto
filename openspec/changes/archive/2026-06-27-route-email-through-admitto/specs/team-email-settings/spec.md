## REMOVED Requirements

### Requirement: Team scope is supported by the unified EmailSettings aggregate
**Reason**: Team-scoped SMTP settings are removed entirely.
**Migration**: Remove team-scoped `EmailSettings` rows and related persistence constraints.

### Requirement: Team-scoped settings act as the fallback in effective-settings resolution
**Reason**: The send path no longer resolves SMTP settings from team/event rows.
**Migration**: Replace fallback logic with system sender configuration.

### Requirement: Team-scoped admin endpoints share the EmailSettings slice family
**Reason**: Team-scoped email settings endpoints are removed.
**Migration**: Remove backend routes, Admin UI proxies, and generated SDK calls for `/admin/teams/{team}/email-settings`.
