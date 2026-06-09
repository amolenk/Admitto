## MODIFIED Requirements

### Requirement: Admin UI event dashboard hero card
The event hero card SHALL display the event name, dates, time, timezone, website URL (if set), status badge, and countdown badge. The hero card SHALL NOT include a "Copy link" or any other shortcut action button in the top-right corner.

#### Scenario: Hero card shows event metadata without action buttons
- **WHEN** an organizer views the event dashboard
- **THEN** the hero card shows the event name, date, time, website URL (if set), status badge, and countdown — and no copy or share button is visible

### Requirement: Admin UI event dashboard check-in card
The check-in card SHALL display check-in timing information, a QR scanner button, and summary statistics (checked-in count, expected count, completion percentage). The check-in card SHALL NOT include a "Share link" or any copy-shortcut button.

#### Scenario: Check-in card shows scanner button without share link
- **WHEN** an organizer views the event dashboard check-in card
- **THEN** the card shows the QR Scanner button and check-in stats, but no "Share link" button is present
