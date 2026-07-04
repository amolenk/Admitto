## ADDED Requirements

### Requirement: Event Email tab exposes a Send-test-email action with a recipient picker

The event Email tab (`/teams/{teamSlug}/events/{eventSlug}/settings/email`) SHALL render a "Send test email" action below the email settings form whenever the event has its own saved email settings. The action SHALL consist of a recipient dropdown and a button. The dropdown SHALL be populated client-side from the team's contact email (read via `GET /api/teams/{teamSlug}`) and the email addresses of the current team members (read via `GET /api/teams/{teamSlug}/members`), with duplicate addresses collapsed and the team's contact email selected by default. The dropdown SHALL NOT include any event-scoped contact list — the recipient set is the same on both scopes.

When the button is clicked, the page SHALL `POST /api/teams/{teamSlug}/events/{eventSlug}/email-settings/test` with the chosen recipient in the body. While the request is in flight, the button SHALL be disabled. On success, the page SHALL render an inline non-destructive `Alert` near the button identifying the recipient ("Test email sent to <address>"). On failure, the page SHALL render an inline destructive `Alert` near the button containing the server's error message. The action SHALL NOT be rendered when the event has no event-scoped settings — even if team-scoped settings exist — because the test endpoint targets only the saved settings of the current scope.

#### Scenario: Action hidden when the event inherits from team
- **GIVEN** team "acme" has team-scoped settings AND event "devconf-2026" has no event-scoped row
- **WHEN** an organizer opens `/teams/acme/events/devconf-2026/settings/email`
- **THEN** the inheritance callout is shown
- **AND** the Send-test-email action is not rendered

#### Scenario: Action shown when event has its own settings
- **GIVEN** event "devconf-2026" has its own event-scoped settings
- **WHEN** an organizer opens `/teams/acme/events/devconf-2026/settings/email`
- **THEN** the Send-test-email action is rendered below the form, alongside the existing Delete action

#### Scenario: Recipient dropdown matches the team page
- **GIVEN** team "acme" has contact email `events@acme.org` and members `alice@example.com`, `bob@example.com`
- **WHEN** an organizer opens the event Email tab for any event owned by "acme"
- **THEN** the recipient dropdown lists exactly the same options as on the team Email page (`events@acme.org`, `alice@example.com`, `bob@example.com`), with `events@acme.org` selected by default

#### Scenario: Successful test
- **GIVEN** recipient `bob@example.com` is selected on the event Email tab
- **WHEN** the organizer clicks "Send test email" and the API responds `200 OK`
- **THEN** the page renders a non-destructive inline alert reading "Test email sent to bob@example.com"

#### Scenario: Failed test surfaces the server error
- **WHEN** the organizer clicks "Send test email" and the API responds with an error containing "Connection refused"
- **THEN** the page renders a destructive inline alert containing "Connection refused"
