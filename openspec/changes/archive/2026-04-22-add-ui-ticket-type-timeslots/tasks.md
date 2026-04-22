## 1. Add ticket type form

- [x] 1.1 Add a `timeSlots: string[]` field to the `addSchema` zod schema and the form's default values in `add-ticket-type-form.tsx`.
- [x] 1.2 Build a small "tag input" UI (using `Input` + `Badge` components already in the project): typed token + Enter/comma adds a chip; backspace on empty input removes the last chip; each chip has an `x` button.
- [x] 1.3 Validate each token client-side against `^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$` before adding it, surface inline error message on the time-slot field for invalid tokens, and disallow duplicates within the form.
- [x] 1.4 Accept a `suggestions: string[]` prop and render the deduped, not-yet-selected suggestions below the input as clickable chips that add the slug as if typed.
- [x] 1.5 Submit `timeSlots: values.timeSlots` (always an array, never `null`) in the `apiClient.post(...)` body.

## 2. Ticket types page wiring

- [x] 2.1 In `page.tsx`, compute `availableTimeSlots = Array.from(new Set(types.flatMap(t => t.timeSlots ?? [])))` and pass it as `suggestions` to `AddTicketTypeForm`.
- [x] 2.2 In `TicketTypeCard`, render each ticket type's `timeSlots` as a row of `<Badge variant="outline">` chips beneath the slug; omit the row when the array is empty.

## 3. Edit ticket type dialog

- [x] 3.1 In `edit-ticket-type-form.tsx`, when `ticketType.timeSlots` is non-empty, render disabled chips and a small helper text: "Time slots can't be changed after creation."
- [x] 3.2 Confirm the submission body still contains only `name` and `maxCapacity` (no `timeSlots` field).

## 4. Verification

- [x] 4.1 Run `cd src/Admitto.UI.Admin && pnpm build` and resolve any TypeScript errors.
- [x] 4.2 Manual smoke test against a running AppHost: add a ticket type with two time slots, confirm chips show on the card and re-open it for edit (read-only chips visible); add another ticket type and confirm suggestions include the previously-used slugs; add a ticket type with no time slots and confirm no badge row is rendered.
