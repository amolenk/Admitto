## ADDED Requirements

### Requirement: Partner can retrieve reduced registration details by registration ID
The system SHALL expose `GET /api/events/{eventSlug}/registrations/{registrationId}` as a Partner API endpoint that requires a valid active `X-Api-Key`.

The endpoint SHALL derive `TeamId` from the authenticated API-key principal, SHALL resolve `{eventSlug}` to the internal ticketed event ID within that team scope, and SHALL retrieve the requested registration only by `registrationId` within that resolved event scope.

The `registrationId` path value SHALL serve as the attendee bearer credential for the registration itself, while the required `X-Api-Key` authorizes the external event site/team integration. The endpoint SHALL NOT require an `Authorization` bearer token or email-verification token.

The response SHALL contain exactly the reduced registration detail payload needed by partner event websites:
- `id`: registration GUID
- `email`: attendee email address
- `firstName`: attendee first name
- `lastName`: attendee last name
- `status`: registration status
- `ticketTypeIds`: current registered ticket type IDs from the registration's stored ticket snapshot
- `tickets`: current registered tickets from the registration's stored ticket snapshot, including at least ticket type ID and display name
- `additionalDetails`: dictionary of string-to-string additional-detail values, empty when no values were provided

The response SHALL NOT include admin-only fields such as registered timestamp, reconfirmation state, reconfirmation timestamp, cancellation reason, or activity log entries.

#### Scenario: Existing registration returns reduced details
- **WHEN** a partner website calls `GET /api/events/devconf/registrations/{registrationId}` with a valid API key for the event's team and the registration belongs to that event
- **THEN** the system returns 200 with the registration's id, email, firstName, lastName, status, ticketTypeIds, tickets, and additionalDetails only

#### Scenario: Additional details are empty when absent
- **WHEN** a partner website retrieves a registration that has no stored additional-detail values
- **THEN** the response contains `additionalDetails` as an empty object

#### Scenario: Admin-only fields are not returned
- **WHEN** a partner website retrieves a registration that has reconfirmation, cancellation, or activity-log metadata
- **THEN** the response omits registered timestamp, reconfirmation state, reconfirmation timestamp, cancellation reason, and activity log entries

#### Scenario: Missing API key is rejected
- **WHEN** a request is made to the Partner registration detail endpoint without `X-Api-Key`
- **THEN** the system returns HTTP 401 and does not run the registration detail query

#### Scenario: Authorization bearer token is not required
- **WHEN** a request is made to the Partner registration detail endpoint with a valid API key, event slug, and registration ID, but no `Authorization` bearer token
- **THEN** the system returns the registration detail response

#### Scenario: API key from another team cannot read registration details
- **WHEN** a request is made with a valid API key for a team that does not own the requested event slug
- **THEN** the endpoint resolves the event within the API key owner's team scope and returns not found

#### Scenario: Unknown registration returns not found
- **WHEN** a partner website requests a registration ID that does not belong to a registration in the resolved event
- **THEN** the system returns not found

#### Scenario: Registration from another event returns not found
- **WHEN** a partner website requests a registration ID that belongs to a registration for a different event than the resolved `{eventSlug}`
- **THEN** the system returns not found
