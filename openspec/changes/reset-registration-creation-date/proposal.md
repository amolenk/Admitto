## Why

Resetting a cancelled registration through the admin-add flow represents a new registration lifecycle, but the aggregate currently retains the original creation timestamp.
This makes creation-time data and downstream behavior reflect the cancelled record rather than the reset registration.

## What Changes

- Reset the `Registration` aggregate's creation timestamp whenever any registration path resets a cancelled registration.
- Preserve the existing registration identifier and the other reset semantics defined for admin-added registrations.
- Cover the reset timestamp behavior with domain-level regression tests.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `admin-registration`: Resetting a cancelled registration also refreshes its creation timestamp.
- `attendee-registration`: Self-service and coupon registration resets also refresh the cancelled aggregate's creation timestamp.

## Impact

- `Admitto.Core` Registrations domain aggregate and every existing registration path that invokes its reset operation.
- Registrations domain tests and existing integration coverage for the admin, self-service, and coupon reset paths.
- No API contract, database schema, cross-module contract, or dependency changes.
