## REMOVED Requirements

### Requirement: Team settings sidebar exposes an Email entry
**Reason**: The team email settings page is removed because organizers no longer configure SMTP settings.
**Migration**: Remove the Email entry from the team settings sidebar unless a future non-SMTP branding page replaces it.

### Requirement: Team Email page shows the team-scoped email settings form
**Reason**: Team-scoped SMTP settings are removed.
**Migration**: Remove the page and form. Team accent color belongs to team/general settings if exposed in UI.

### Requirement: Team Email form saves via the team-scoped admin endpoint
**Reason**: The team-scoped email settings endpoint is removed.
**Migration**: Remove client mutations and generated SDK/proxy calls for the endpoint.

### Requirement: Team Email page supports deleting the team-scoped row
**Reason**: There is no team-scoped email settings row to delete.
**Migration**: Remove delete UI and related proxy/API calls.

### Requirement: Admin UI exposes a Next.js proxy route for team-scoped email settings
**Reason**: The backend team email settings endpoint is removed.
**Migration**: Delete the proxy route after regenerating the Admin UI SDK.

### Requirement: Team Email page exposes a Send-test-email action with a recipient picker
**Reason**: Organizer SMTP diagnostic testing is removed.
**Migration**: Remove the send-test-email UI, proxy route, and generated SDK call.
