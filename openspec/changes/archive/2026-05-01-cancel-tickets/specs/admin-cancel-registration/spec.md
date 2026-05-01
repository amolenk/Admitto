## Capability: admin-cancel-registration

### Summary

Admins can cancel an individual registration via `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/cancel`. A `reason` field is required and must be one of the two admin-selectable values: `AttendeeRequest` or `VisaLetterDenied`. `TicketTypesRemoved` is an internal system reason and cannot be supplied by an admin caller.

### Acceptance Scenarios

**SC001_CancelRegistration_WithAttendeeRequestReason_Returns204**
Given a valid registration in state `Registered`
When an admin POSTs to the cancel endpoint with `reason: AttendeeRequest`
Then the response is 204 and the registration state changes to `Cancelled`.

**SC002_CancelRegistration_WithVisaLetterDeniedReason_Returns204**
Given a valid registration in state `Registered`
When an admin POSTs to the cancel endpoint with `reason: VisaLetterDenied`
Then the response is 204 and the registration state changes to `Cancelled`.

**SC003_CancelRegistration_AlreadyCancelled_Returns409**
Given a registration that is already in state `Cancelled`
When an admin POSTs to the cancel endpoint with any valid reason
Then the response is 409 Conflict.

**SC004_CancelRegistration_RegistrationNotFound_Returns404**
Given a `registrationId` that does not exist for the given event
When an admin POSTs to the cancel endpoint
Then the response is 404 Not Found.

**SC005_CancelRegistration_MissingReason_Returns400**
When an admin POSTs to the cancel endpoint without a `reason` field
Then the response is 400 Bad Request with a validation error on `reason`.

**SC006_CancelRegistration_InvalidReason_Returns400**
When an admin POSTs to the cancel endpoint with `reason: TicketTypesRemoved` or any unknown string
Then the response is 400 Bad Request.

**SC007_CancelRegistration_WrongTeamOrEvent_Returns404**
Given a `registrationId` that exists but belongs to a different event
When an admin POSTs to the cancel endpoint
Then the response is 404 Not Found.

**SC008_CLI_CancelRegistration_WithReason_Succeeds**
Given a valid registration
When an admin runs `admitto event registration cancel <id> --reason AttendeeRequest` (or `VisaLetterDenied`)
Then the command succeeds and prints a confirmation message.

**SC009_CLI_CancelRegistration_MissingReason_ShowsError**
When an admin runs `admitto event registration cancel <id>` without `--reason`
Then the CLI shows a validation error.

**SC010_CLI_CancelRegistration_InvalidReason_ShowsError**
When an admin runs `admitto event registration cancel <id> --reason TicketTypesRemoved` or an unknown value
Then the CLI shows a validation error.
