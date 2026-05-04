## MODIFIED Requirements

### Requirement: Registration tab manages registration policy and ticket types
The Registration tab ticket type add and edit forms SHALL include:
- An **"Enable self-service registration"** checkbox (default: checked). When unchecked, the ticket type is only accessible via admin registration or coupon.
- A **"Limit capacity"** checkbox. When unchecked, the capacity is unlimited (null). When checked, a positive integer capacity input is revealed. This replaces the plain optional capacity number input, fixing the inability to clear a capacity once set.

The ticket type list row SHALL display a visual indicator (e.g., a badge or icon) showing whether self-service is enabled or disabled for each ticket type.

#### Scenario: Add ticket type with self-service enabled and capacity limit
- **WHEN** an organizer checks "Enable self-service registration", checks "Limit capacity", enters 200, and submits
- **THEN** the ticket type is created with `selfServiceEnabled: true` and `maxCapacity: 200`

#### Scenario: Add ticket type with self-service disabled
- **WHEN** an organizer unchecks "Enable self-service registration" and submits
- **THEN** the ticket type is created with `selfServiceEnabled: false`

#### Scenario: Add ticket type with unlimited self-service capacity
- **WHEN** an organizer checks "Enable self-service registration", leaves "Limit capacity" unchecked, and submits
- **THEN** the ticket type is created with `selfServiceEnabled: true` and `maxCapacity: null`

#### Scenario: Remove capacity limit on existing ticket type
- **WHEN** an organizer edits a ticket type that has a capacity of 200, unchecks "Limit capacity", and saves
- **THEN** the ticket type is updated with `maxCapacity: null` (unlimited)

#### Scenario: Self-service indicator shown in ticket type list
- **WHEN** an organizer views the Registration tab with ticket types "general" (selfServiceEnabled: true) and "vip" (selfServiceEnabled: false)
- **THEN** each row shows a distinct visual indicator for self-service status
