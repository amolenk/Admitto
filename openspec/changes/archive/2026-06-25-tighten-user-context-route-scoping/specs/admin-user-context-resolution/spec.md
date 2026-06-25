## ADDED Requirements

### Requirement: Admin JWT requests resolve domain user context before authorization
The system SHALL resolve every authenticated JWT admin request to an Admitto domain user context before endpoint authorization and endpoint handlers execute. The resolved context SHALL be cached for the duration of the request and SHALL be available through `IUserContextAccessor.Current`.

#### Scenario: Known user reaches authorization with cached context
- **WHEN** an authenticated JWT request identifies a known Admitto user
- **THEN** the request has a cached user context before authorization handlers run

#### Scenario: Unknown user is rejected before endpoint execution
- **WHEN** an authenticated JWT request cannot be resolved to an Admitto user
- **THEN** the request is rejected with `403 Forbidden` before endpoint handlers run

#### Scenario: First sign-in binds external identity before endpoint execution
- **WHEN** an authenticated JWT request has an external subject and matching pre-invited user email without an external identity
- **THEN** the user's external identity is bound and the resolved context is cached before endpoint handlers run

### Requirement: Admin route scope is classified before user context resolution
The system SHALL classify admin JWT request route scope as global, team-scoped, or event-scoped before resolving domain user context. Global scope SHALL contain no `teamId` or `eventId`; team scope SHALL contain a valid `teamId`; event scope SHALL contain both a valid `teamId` and a valid `eventId`.

#### Scenario: Global admin route resolves with global scope
- **WHEN** an authenticated JWT request targets an admin route with no `teamId` and no `eventId`
- **THEN** user context resolution uses global scope

#### Scenario: Team admin route resolves with team scope
- **WHEN** an authenticated JWT request targets an admin route with a valid `teamId` and no `eventId`
- **THEN** user context resolution uses team scope for that team

#### Scenario: Event admin route resolves with event scope
- **WHEN** an authenticated JWT request targets an admin route with valid `teamId` and `eventId` values
- **THEN** user context resolution uses event scope for that team and event

#### Scenario: Event route without team is rejected
- **WHEN** an authenticated JWT request has an `eventId` route value without a `teamId` route value
- **THEN** the request is rejected with `403 Forbidden` before authorization handlers run

#### Scenario: Invalid route scope value is rejected
- **WHEN** an authenticated JWT request has an unparsable `teamId` or `eventId` route value
- **THEN** the request is rejected with `403 Forbidden` before authorization handlers run

### Requirement: Event scoped admin requests validate event ownership before endpoint execution
The system SHALL verify that an event-scoped admin JWT request's `eventId` belongs to the route `teamId` before endpoint handlers execute. Non-admin users SHALL be rejected with `403 Forbidden` when the event does not belong to the route team.

#### Scenario: Event belongs to route team
- **WHEN** a non-admin authenticated JWT request targets an event-scoped route and the event belongs to the route team
- **THEN** user context resolution succeeds and authorization can evaluate the resolved context

#### Scenario: Event does not belong to route team
- **WHEN** a non-admin authenticated JWT request targets an event-scoped route and the event does not belong to the route team
- **THEN** the request is rejected with `403 Forbidden` before endpoint handlers run

#### Scenario: Admin bypasses event ownership guard
- **WHEN** an admin authenticated JWT request targets an event-scoped route and the event does not belong to the route team
- **THEN** user context resolution succeeds with admin context

### Requirement: API key requests bypass admin user context resolution
The system SHALL NOT resolve JWT domain user context for API-key authenticated public requests. Public API-key requests SHALL continue deriving team scope from the authenticated `team_id` claim.

#### Scenario: API key request skips JWT context resolver
- **WHEN** a public request authenticates using `X-Api-Key`
- **THEN** admin user context resolution is skipped and public endpoint code obtains team scope from the API-key principal
