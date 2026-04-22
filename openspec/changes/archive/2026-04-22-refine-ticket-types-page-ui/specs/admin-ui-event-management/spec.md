## ADDED Requirements

### Requirement: Ticket types page header shows the event name
The Admin UI Ticket Types page SHALL display the current event's name as the page title (in the same large heading slot previously occupied by "Tickets"). While the event details are loading, the page SHALL fall back to the event slug.

#### Scenario: Header shows event name
- **WHEN** an organizer opens the Ticket Types page for event "devconf-2026" whose name is "DevConf 2026"
- **THEN** the page heading displays "DevConf 2026"

#### Scenario: Header falls back to slug while loading
- **WHEN** the Ticket Types page renders before the event details have loaded
- **THEN** the page heading displays the event slug

---

### Requirement: Ticket types page uses "registered" wording for free-event ticketing
The Admin UI Ticket Types page SHALL use the verb "registered" (and its noun form "Registered") in place of "sold"/"Sold" everywhere on the page. This applies to:

- The header summary line ("N registered of M across K ticket types").
- The per-card stat label ("Registered" instead of "Sold").
- Any percentage or sub-label associated with capacity ("X% registered").

#### Scenario: Header summary uses "registered"
- **WHEN** the Ticket Types page renders with totals 12 registered out of 100 across 3 ticket types
- **THEN** the summary line reads "12 registered of 100 across 3 ticket types"

#### Scenario: Card stat label uses "Registered"
- **WHEN** any ticket type card is rendered
- **THEN** the leftmost stat in the three-column block is labelled "Registered" (not "Sold")

---

### Requirement: Available ticket types use "Available" badge text
The Admin UI Ticket Types page SHALL render the in-sale status badge with the text "Available" instead of "On sale". The visual styling and the conditions for showing it (active and not at capacity) SHALL remain unchanged.

#### Scenario: Active, in-stock ticket type shows "Available"
- **WHEN** a card renders an active ticket type with remaining capacity
- **THEN** the status badge text reads "Available"

---

### Requirement: Ticket type cards expose actions only via the overflow menu
The Admin UI Ticket Types page SHALL expose Edit and Cancel actions for a ticket type only via the per-card `…` overflow menu. The card SHALL NOT render an inline footer action bar containing duplicate Edit / Cancel buttons.

#### Scenario: No footer action bar
- **WHEN** an active (not cancelled) ticket type card is rendered
- **THEN** there is no row of inline Edit / Cancel buttons beneath the stats; the only edit/cancel entry point is the `…` overflow menu in the card header

#### Scenario: Cancelled ticket type still hides actions
- **WHEN** a cancelled ticket type card is rendered
- **THEN** the overflow menu offers no edit or cancel actions (unchanged behaviour) and there is still no footer action bar

---

### Requirement: Ticket type cards omit the slug
The Admin UI Ticket Types page SHALL NOT display the ticket type slug on the card. The slug SHALL remain visible in the Edit ticket type dialog (as the immutable identifier shown there today).

#### Scenario: Card hides slug
- **WHEN** a ticket type card is rendered for ticket type slug "vip"
- **THEN** the card does not show the text "vip" anywhere; the name and (when present) time-slot badges are the only identifying labels in the card header

---

### Requirement: Cancel action is labelled "Cancel ticket type"
The Admin UI Ticket Types page SHALL label the cancel action as "Cancel ticket type" in the `…` overflow menu (replacing any previous label such as "Cancel" or "Cancel sales"). Confirmation dialogs and toasts that surface the action SHALL use the same wording.

#### Scenario: Overflow menu shows "Cancel ticket type"
- **WHEN** an organizer opens the `…` menu on an active ticket type card
- **THEN** the destructive item reads "Cancel ticket type"

---

### Requirement: Ticket type cards have a subtle ticket-stub appearance
The Admin UI Ticket Types page SHALL style ticket type cards with (a) a noticeably rounded outer border-radius and (b) a single horizontal perforated/dashed divider with rounded notches on the left and right edges separating the card header (name + status badge) from the stats region, evoking a tear-off ticket stub. The treatment SHALL be implemented with CSS only (no SVG/illustration assets) and SHALL NOT change the card's content layout or grid placement.

#### Scenario: Card shows perforated divider
- **WHEN** a ticket type card is rendered
- **THEN** a dashed/perforated horizontal line with rounded edge notches is visible inside the card, separating the header (name + badge) from the stats region

#### Scenario: No layout shift versus prior card
- **WHEN** comparing the new card to the prior card at the same viewport width
- **THEN** the grid columns, card width, and stat block remain unchanged; only the border-radius, vertical padding, and divider treatment differ
