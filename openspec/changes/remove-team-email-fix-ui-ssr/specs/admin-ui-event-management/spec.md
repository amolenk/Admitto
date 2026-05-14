## MODIFIED Requirements

### Requirement: Admin UI exposes event settings through tabbed navigation
The Admin UI SHALL render event settings under `/teams/{teamId}/events/{eventId}/settings` with a side-navigation containing tabs: **General**, **Registration**, **Cancellation**, **Reconfirmation**, **Email**, **Email templates**, and **Danger zone**. The active tab SHALL be highlighted. Each tab SHALL be an independently routable page. The layout shell (breadcrumbs, heading, sidebar nav) SHALL be rendered as a Next.js Server Component so that the team name and event name are fetched server-side and present in the initial HTML.

#### Scenario: Navigate between tabs
- **WHEN** an organizer is on the General tab and clicks the "Registration" tab
- **THEN** the URL changes to `.../settings/registration` and the Registration tab content loads

#### Scenario: Active tab is highlighted
- **WHEN** the Email tab is the current page
- **THEN** the "Email" navigation entry is rendered with the active style

#### Scenario: Team and event names are present on initial render
- **WHEN** an organizer navigates directly to any event settings tab URL or hard-refreshes the page
- **THEN** the breadcrumb shows the team name and event name immediately, without any GUID flash or loading state
