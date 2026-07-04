## ADDED Requirements

### Requirement: Team Email page exposes a Send-test-email action with a recipient picker

The Team Email page (`/teams/{teamSlug}/settings/email`) SHALL render a "Send test email" action below the email settings form whenever the team has saved email settings. The action SHALL consist of a recipient dropdown and a button. The dropdown SHALL be populated client-side from the team's contact email (read via `GET /api/teams/{teamSlug}`) and the email addresses of the current team members (read via `GET /api/teams/{teamSlug}/members`), with duplicate addresses collapsed and the team's contact email selected by default.

When the button is clicked, the page SHALL `POST /api/teams/{teamSlug}/email-settings/test` with the chosen recipient in the body. While the request is in flight, the button SHALL be disabled. On success, the page SHALL render an inline non-destructive `Alert` near the button identifying the recipient ("Test email sent to <address>"). On failure, the page SHALL render an inline destructive `Alert` near the button containing the server's error message. The action SHALL NOT be rendered when no team-scoped settings exist (i.e. the GET returned `404`).

#### Scenario: Action hidden until team has saved settings
- **GIVEN** team "acme" has no team-scoped email settings (the GET returns `404`)
- **WHEN** an organizer opens `/teams/acme/settings/email`
- **THEN** the page does not render the Send-test-email action

#### Scenario: Recipient dropdown is populated from team + members
- **GIVEN** team "acme" has team-scoped email settings, a contact email `events@acme.org`, and members "alice@example.com" and "bob@example.com"
- **WHEN** an organizer opens `/teams/acme/settings/email`
- **THEN** the recipient dropdown lists `events@acme.org`, `alice@example.com`, and `bob@example.com` exactly once
- **AND** `events@acme.org` is selected by default

#### Scenario: Successful test
- **GIVEN** the recipient `alice@example.com` is selected
- **WHEN** the organizer clicks "Send test email" and the API responds `200 OK`
- **THEN** the page renders a non-destructive inline alert reading "Test email sent to alice@example.com"

#### Scenario: Failed test surfaces the server error
- **WHEN** the organizer clicks "Send test email" and the API responds with an error containing "Authentication failed"
- **THEN** the page renders a destructive inline alert containing "Authentication failed"
- **AND** the alert remains visible until the user retries or dismisses it
