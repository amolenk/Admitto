# Admin UI Attendee Detail Specification

## Purpose

This capability covers the attendee detail page in the Admin UI: a dedicated page reachable from the registrations table that shows all information known about a single attendee — their profile, ticket types, activity timeline, and email history — with a real cancel action and a placeholder "Change ticket types" button.

## UI Design Guidance

The mockup for this page is embedded in `design/Admitto Admin.html` — open it in a browser and click any registration row to reach the attendee page. The relevant source components are `AttendeePage`, `AttendeeDetails`, `AttendeeTickets`, and `AttendeeActivity` (lines ~1170–1390 in the HTML source).

Key layout and design decisions from the mockup:

- **Page header**: Breadcrumb back-link ("← Registrations / Alice Smith") + top-level hero card with avatar initials, name, status badge, reconfirmed badge, email/company/registered-at metadata row, and action buttons (Cancel in destructive outline style, visible only when not Cancelled).
- **Two-column grid**: `grid-cols-12` — left column (`col-span-5 lg`) stacks `AttendeeDetails` card then `AttendeeTickets` card; right column (`col-span-7 lg`) holds the combined `AttendeeActivity` card.
- **Merged activity + emails feed**: Activity events (Registered, Reconfirmed, Cancelled) and emails are rendered in a **single chronological timeline** — NOT as separate sections. The timeline has tab filters: All | Events | Emails. Each entry has a coloured icon, title, detail text, and relative timestamp. Email entries additionally show "View" and "Resend" ghost buttons.
- **Timeline icon colours**: `registered` → primary, `reconfirmed` → success, `cancelled` → destructive, `email` → muted-foreground. Icon backgrounds are a 6% tint of the respective colour.
- **Tickets card**: Shows one ticket panel per ticket with name, slug, Active/Released badge, and a "Change" ghost button in the card header.
- **Details card**: `<dl>` grid with `130px` label column and truncating value column, divided by hairline separators.
- **Component library**: shadcn/ui from `app/components/ui/` — `Card`, `Badge`, `Button`, `Skeleton`, `AlertDialog`, `Select`, `Separator`. No table needed for emails (they're timeline entries).
- **Cancel dialog**: `AlertDialog` pattern from `registrations/page.tsx`. Reason selector uses `<Select>`. Restrict to `AttendeeRequest` and `VisaLetterDenied` (do NOT show `TicketTypesRemoved`).
- **Data fetching**: `useQuery` + `apiClient` + `useParams`. Two parallel fetches: registration detail and attendee emails; merge results client-side for the timeline.
- **Toast / notifications**: `toast` from `sonner`.
- **Design tokens reference**: `design/dashboard.jsx` and `design/settings.jsx` for `eyebrow`, `card`, `btn`, `badge` class patterns.

## Requirements

### Requirement: Admin UI provides an attendee detail page for each registration

The Admin UI SHALL provide a page at `/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}` that loads and renders full details for a single registration.

The page SHALL be structured as follows:

**Hero header card:**
- Avatar circle with initials
- Full name (first + last, falling back to email prefix), status badge, reconfirmed badge (if applicable)
- Secondary row: email, registered-at timestamp
- Action buttons: "Cancel registration" (destructive outline, visible only when `status=Registered`), placeholder "Change ticket types"

**Two-column body (5/7 split on large screens, stacked on small):**

Left column:
- **Attendee details card**: `<dl>` list of fields — full name, email, registration ID, status, reconfirmed. Additional details key/value pairs appended when `additionalDetails` is non-empty.
- **Tickets card**: One panel per ticket showing name, slug, Active/Released badge, and a placeholder "Change" button in the card header.

Right column:
- **Combined "Activity & emails" timeline card**: A single chronological feed merging `ActivityLog` entries (from the registration detail response) and email log entries (from the attendee-emails response). Ordered most-recent first. Each entry has a coloured icon, title, detail text, and timestamp. Email entries additionally show "View" (no-op placeholder) and "Resend" (no-op placeholder) ghost buttons. A tab filter (All | Events | Emails) lets the admin narrow the feed.

**Actions:**
- "Cancel registration" button in the hero card: visible when `status=Registered`; clicking opens a confirmation dialog where the admin selects a cancellation reason (`AttendeeRequest` or `VisaLetterDenied`) and confirms; on confirm, calls `POST /admin/teams/{teamSlug}/events/{eventSlug}/registrations/{registrationId}/cancel`; on success, the page data is refreshed to reflect the updated status; on error, an error message is shown.
- No cancel button when `status=Cancelled`.

The page SHALL show loading skeletons while data is being fetched and surface an error state if either fetch fails.

#### Scenario: SC001 Page renders attendee details for a registered attendee

- **GIVEN** an organizer navigates to the attendee detail page for a registration with `firstName="Alice"`, `lastName="Smith"`, `status=Registered`, `hasReconfirmed=false`, and one ticket "Early bird"
- **THEN** the page shows "Alice Smith", the Registered badge, "—" in the reconfirm field, and the "Early bird" ticket; the "Cancel registration" button is visible

#### Scenario: SC002 Page renders additional details

- **GIVEN** a registration has `additionalDetails={"dietary":"vegan","tshirt":"M"}`
- **WHEN** the page renders
- **THEN** the additional details section shows two rows: "dietary: vegan" and "tshirt: M"

#### Scenario: SC003 Page hides additional details section when empty

- **GIVEN** a registration has no additional detail fields
- **WHEN** the page renders
- **THEN** no additional details section is shown

#### Scenario: SC004 Activity timeline shows registration and reconfirmation milestones from ActivityLog

- **GIVEN** the registration detail response includes `activities`: `[{type=Registered, occurredAt=T1}, {type=Reconfirmed, occurredAt=T2}]`
- **WHEN** the page renders
- **THEN** the activity timeline shows "Registered" at T1 and "Reconfirmed" at T2, ordered most-recent first (T2 first)

#### Scenario: SC005 Activity timeline shows cancellation milestone with reason

- **GIVEN** the `activities` array includes a `{type=Cancelled, occurredAt=T3, metadata="AttendeeRequest"}` entry
- **WHEN** the page renders
- **THEN** the timeline shows a "Cancelled (Attendee request)" milestone; the "Cancel registration" button is not shown

#### Scenario: SC006 Activity feed shows email entries interleaved with registration events

- **GIVEN** two emails were sent to the attendee: "Ticket confirmation" (Sent) and "Reconfirmation request" (Sent), and the `activities` array has Registered and Reconfirmed entries
- **WHEN** the page renders with "All" tab active
- **THEN** the combined feed shows all four entries ordered most-recent first, each with appropriate icon colour

#### Scenario: SC006b Emails tab filters to email entries only

- **GIVEN** the combined feed contains both activity and email entries
- **WHEN** an admin clicks the "Emails" tab
- **THEN** only email log entries are shown

#### Scenario: SC007 Activity feed shows empty state when no entries

- **GIVEN** no activity log entries and no emails exist for the attendee
- **WHEN** the page renders
- **THEN** the timeline shows an empty-state message such as "Nothing here yet."

#### Scenario: SC008 Loading state shows skeletons

- **WHEN** the page is fetching data
- **THEN** skeleton placeholders are displayed instead of empty content

#### Scenario: SC009 Cancel registration opens confirmation dialog with reason selection

- **GIVEN** the registration status is `Registered`
- **WHEN** an admin clicks the "Cancel registration" button
- **THEN** a confirmation dialog opens with a reason selector showing `AttendeeRequest` and `VisaLetterDenied`; no API call is made until the admin confirms

#### Scenario: SC009b Cancel confirmed calls cancel endpoint and refreshes page

- **GIVEN** the cancel confirmation dialog is open with reason `AttendeeRequest` selected
- **WHEN** the admin confirms the cancellation
- **THEN** `POST …/cancel` is called with the selected reason; on success, the page data refreshes and the button disappears (status is now Cancelled)

#### Scenario: SC010 Change ticket types button shows Coming soon notification

- **WHEN** an admin clicks the "Change ticket types" button
- **THEN** a "Coming soon" notification is displayed and no API call is made

### Requirement: Page back-navigates to the registrations list

The attendee detail page SHALL include a back link or breadcrumb that navigates the admin back to the registrations list for the same event.

#### Scenario: SC011 Back link returns to registrations list

- **WHEN** an admin clicks the back link on the attendee detail page
- **THEN** the browser navigates to `/teams/{teamSlug}/events/{eventSlug}/registrations`
