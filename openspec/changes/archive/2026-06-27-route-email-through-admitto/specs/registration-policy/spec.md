## ADDED Requirements

### Requirement: Registration policy is not gated by organizer email settings
Registration policy configuration and registration-open status SHALL NOT depend on team-scoped or event-scoped organizer email settings. Application email is platform configured through the Admitto system sender, so missing organizer SMTP settings SHALL NOT block opening or configuring registration.

#### Scenario: Configure registration policy without team email settings
- **WHEN** an organizer configures a registration policy for an active event and no team email settings row exists
- **THEN** the policy configuration is accepted when all registration-policy rules pass

#### Scenario: Open-status response does not require email settings
- **WHEN** the admin UI queries registration-open status for an active event with a valid registration window and no organizer SMTP settings
- **THEN** the response is based on the registration policy and event lifecycle, not on email-settings existence
