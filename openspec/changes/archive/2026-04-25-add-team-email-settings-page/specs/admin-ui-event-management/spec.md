## MODIFIED Requirements

### Requirement: Email tab manages event email server settings

The Email tab SHALL show a form for the event's email settings (SMTP host, port, from-address, authentication mode, and credentials when applicable). Existing secret fields (e.g. password) SHALL be displayed masked, and updating them SHALL require re-entry rather than reusing the masked value. The form SHALL submit with the email-settings `Version` for optimistic concurrency. After a successful save the Email tab SHALL display "Email is configured".

The Email tab SHALL also surface the relationship between event-scoped and team-scoped settings by querying the team-scoped admin endpoint (`GET /admin/teams/{teamSlug}/email-settings`) in parallel with the event-scoped GET, treating `404` as "no row". The result SHALL drive an inheritance callout displayed alongside the form:

- When team-scoped settings exist AND the event has no event-scoped row, the callout SHALL read "Inherited from team settings" and SHALL link to `/teams/{teamSlug}/settings/email`. The form SHALL render in its empty/unconfigured state and remain editable so the organizer can create an event-scoped override.
- When team-scoped settings exist AND the event has its own event-scoped row, the callout SHALL read "Overriding team settings" and SHALL link to `/teams/{teamSlug}/settings/email`. The form SHALL render pre-filled from the event-scoped row.
- When team-scoped settings do NOT exist, the callout SHALL NOT be displayed (existing behaviour).

The callout is informational only — it SHALL NOT change save or delete behaviour, which continue to operate on the event-scoped row exclusively.

#### Scenario: Configure email settings for the first time

- **WHEN** an organizer fills in SMTP host "smtp.acme.org", port 587, from-address "events@acme.org", username "noreply", password "•••••" and submits
- **THEN** the email settings are saved and the Email tab shows "Email is configured"

#### Scenario: Secret fields are masked on read

- **WHEN** an organizer reopens the Email tab after saving credentials
- **THEN** the password field is rendered masked and empty; the user must re-enter to change it

#### Scenario: Update non-secret fields without re-entering password

- **WHEN** an organizer changes only the from-address and submits without re-entering the password
- **THEN** the from-address is updated and the stored password is unchanged

#### Scenario: Inherited callout shown when only team-scoped settings exist

- **WHEN** an organizer opens the Email tab for event "devconf-2026" (team "acme") and the team-scoped GET returns settings while the event-scoped GET returns `404`
- **THEN** the page shows an "Inherited from team settings" callout linking to `/teams/acme/settings/email`, and the form renders empty

#### Scenario: Overriding callout shown when both scopes exist

- **WHEN** an organizer opens the Email tab and both the team-scoped and event-scoped GETs return settings
- **THEN** the page shows an "Overriding team settings" callout linking to `/teams/acme/settings/email`, and the form is pre-filled from the event-scoped row

#### Scenario: No callout when team-scoped settings do not exist

- **WHEN** an organizer opens the Email tab and the team-scoped GET returns `404` (regardless of whether the event-scoped GET returns `404` or settings)
- **THEN** the page does not show any inheritance callout

#### Scenario: Callout link navigates to team Email page

- **WHEN** an organizer clicks the link inside the inheritance callout on the event Email tab for team "acme"
- **THEN** the browser navigates to `/teams/acme/settings/email`
