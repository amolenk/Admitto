## MODIFIED Requirements

### Requirement: Admin UI exposes event settings through a tabbed Edit Event page

The Admin UI SHALL provide a tabbed **Edit Event** page at `/teams/{teamId}/events/{eventId}/edit` accessible from the event sidebar as the second item after Dashboard. The page SHALL have three tabs implemented as independently routable sub-pages:

- **General** at `/teams/{teamId}/events/{eventId}/edit/general` — general event details (name, dates, etc.)
- **Policies** at `/teams/{teamId}/events/{eventId}/edit/policies` — registration policy, additional detail fields, and reconfirmation policy on a single scrollable page
- **Danger zone** at `/teams/{teamId}/events/{eventId}/edit/danger` — destructive actions

The bare `/edit` path SHALL redirect to `/edit/general`. The active tab SHALL be visually highlighted. There is no shared settings sub-nav or sub-layout; the tab bar is part of the Edit Event page layout itself.

The old `settings/*` URL patterns SHALL permanently redirect (HTTP 308) to their corresponding new paths.

After a successful event creation the UI SHALL navigate to `/teams/{teamId}/events/{eventId}/edit/general` (was `settings`).

#### Scenario: Edit Event page is accessible from the sidebar

- **WHEN** an organizer clicks "Edit Event" in the event sidebar
- **THEN** the browser navigates to `/teams/{teamId}/events/{eventId}/edit/general` and the General tab content is displayed

#### Scenario: Switching to Policies tab

- **WHEN** an organizer clicks the "Policies" tab on the Edit Event page
- **THEN** the URL changes to `.../edit/policies` and a single page shows both the registration policy form and the reconfirmation policy form

#### Scenario: Switching to Danger zone tab

- **WHEN** an organizer clicks the "Danger zone" tab
- **THEN** the URL changes to `.../edit/danger` and the danger zone actions are shown

#### Scenario: Old settings URL redirects to General tab

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/edit/general`

#### Scenario: Old settings/registration URL redirects to Policies tab

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings/registration`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/edit/policies`

#### Scenario: Old settings/reconfirm URL redirects to Policies tab

- **WHEN** a browser navigates to `/teams/acme/events/devconf-2026/settings/reconfirm`
- **THEN** the browser is permanently redirected to `/teams/acme/events/devconf-2026/edit/policies`

#### Scenario: Post-creation redirect lands on General tab

- **WHEN** an organizer completes the create-event flow and the event is successfully created
- **THEN** the UI navigates to `/teams/{teamId}/events/{eventId}/edit/general`

## REMOVED Requirements

### Requirement: Admin UI exposes event settings through tabbed navigation

**Reason**: Replaced by the tabbed Edit Event page. The `settings/` sub-layout with its own left-side sub-nav introduced a nested navigation layer that conflicted with the sidebar-driven layout pattern. See "Admin UI exposes event settings through a tabbed Edit Event page" for the replacement.

**Migration**: All `settings/*` paths are permanently redirected to their new locations (see design.md redirect table).
