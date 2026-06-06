## ADDED Requirements

### Requirement: Coupon operations are scoped to the owning team
All commands and queries that operate on a `Coupon` SHALL verify that the coupon belongs to the team identified by `teamId` in the route. If the coupon does not exist or belongs to a different team, the system SHALL return 404.

This applies to: list coupons, view coupon details, revoke coupon.

#### Scenario: Coupon operation on coupon belonging to the requested team
- **WHEN** an organizer of team "team-a" requests details for a coupon that belongs to "team-a"
- **THEN** the request succeeds and returns the coupon details

#### Scenario: Coupon operation on coupon belonging to a different team
- **WHEN** an organizer of team "team-a" requests details for a coupon that belongs to "team-b"
- **THEN** the request returns 404
