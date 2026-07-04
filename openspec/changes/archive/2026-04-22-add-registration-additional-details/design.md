## Context

After `redesign-ticketed-event-ownership`, the `TicketedEvent` aggregate lives in the Registrations module and owns the registration policy (and related lifecycle). The legacy codebase modelled per-event "additional details" via a small value object (`AdditionalDetailSchema(Name, MaxLength, IsRequired)`) on the event, with values stored on the attendee. Today's modular monolith has no equivalent, and event organizers have no way to ask attendees for free-form information beyond name/email.

This change reintroduces additional details, simplified to match the agreed product scope: every field is optional at the platform layer, every value is a string, the event website is responsible for any "required" semantics or richer validation, and admins manage the schema from the existing registration policy page.

## Goals / Non-Goals

**Goals:**
- Per-event, ordered list of additional detail fields, configurable by team admins.
- Optional, string-typed values collected at registration time (self-register and register-with-coupon) and persisted on the registration aggregate.
- Schema is exposed on the public event-info endpoint so the event website can render the inputs (and apply its own "required"/"format" rules).
- Schema editor lives on the existing admin registration policy page.
- Removing a field preserves historical values for audit/export but stops collecting it on new registrations.
- Admin attendee/registration views show both current and historical (orphaned) additional-detail values.

**Non-Goals:**
- No platform-level "required field" semantics — the event website enforces required-ness.
- No non-string types (no booleans, enums, dates, file uploads). All values are strings.
- No conditional fields, branching, or per-ticket-type field sets.
- No bulk import/export of schema definitions in this change (CLI commands cover the basics).
- Email templates and rendering of additional details inside emails are out of scope (tracked separately).

## Decisions

### Field identity uses a stable `Key`, separate from the human-readable `Name`

Rationale: An admin must be able to rename a field without losing existing values or breaking the event website's HTML field name. Using a stable, slug-like `Key` (kebab-case, generated from the first `Name` and editable via "advanced" UI) decouples display label from storage identity. Renaming `Name` does not affect storage or the public payload key.

Alternative considered: index-based identity (rely on order). Rejected — reordering would silently move values between fields.

### Schema lives on `TicketedEvent` (Registrations module)

Rationale: After the recent redesign, `TicketedEvent` owns all per-event configuration in the Registrations module, including `RegistrationPolicy`. The additional-detail schema is per-event configuration that gates registration data; co-locating it avoids cross-module coordination and lets the schema participate naturally in optimistic concurrency on the event aggregate.

The schema is modelled as a value object on the aggregate (`AdditionalDetailSchema` containing an ordered `IReadOnlyList<AdditionalDetailField>`), updated via a single `UpdateAdditionalDetailSchema` admin command that applies the new list atomically.

### Storage on registration: typed value object backed by a JSONB column

`Registration` gains an `AdditionalDetails` value object (an `IReadOnlyDictionary<string, string>` keyed by field `Key`). EF maps it to a single `jsonb` column on the `registrations` table.

Rationale: JSONB keeps the schema flexible, avoids per-field column changes when admins edit the schema, and supports preserving historical values when fields are removed (the JSONB document simply contains keys no longer present in the schema). PostgreSQL still allows ad-hoc querying when needed.

Alternative considered: child table (`registration_additional_details(registration_id, key, value)`). Rejected — unnecessary normalization for a small key/value bag and harder to keep in lockstep with aggregate-level invariants.

### Validation rules

When `SelfRegisterAttendee` (or `RegisterWithCoupon`) executes:
- The handler reads the event's current schema.
- Any **unknown key** in the request payload is rejected with a validation error (`AdditionalDetailKeyNotInSchema`). This guards against silently dropping data.
- Any value longer than the field's `MaxLength` is rejected (`AdditionalDetailValueTooLong`).
- Any **declared key not present** in the payload is treated as not provided (no error). The platform never enforces presence.
- Empty strings are accepted and stored as empty strings (event website can decide to reject if it wants required-ness).

Schema editing rules on `TicketedEvent.UpdateAdditionalDetailSchema`:
- Field `Name` must be non-empty, ≤ 100 chars; unique within the schema (case-insensitive).
- Field `Key` must match `^[a-z0-9][a-z0-9-]{0,49}$`; unique within the schema; immutable once persisted (admins can't re-key a field — they remove it and add a new one, with the documented "preserve historical values" behaviour applying).
- `MaxLength` is in `[1, 4000]`.
- Reducing `MaxLength` below the longest value already stored for that key on any registration is **allowed** — historical values remain untouched (consistent with the "preserve historical values" rule); only future writes are constrained. This avoids surprising edit failures when the admin doesn't know the data distribution.

### Updating the schema concurrently with active registrations

The schema is part of the event aggregate, so it participates in optimistic concurrency via `TicketedEvent.Version`. Self-registrations operate on `TicketCatalog` snapshots for ticket-type pricing/capacity; for additional-detail validation, they re-read the current event schema at handler time (no snapshotting needed — the schema is small and read-mostly). Race condition: an admin removes a field at the same moment a public registration submits a value for that key. Outcome: the registration's submission is rejected with `AdditionalDetailKeyNotInSchema` because the key is no longer in the current schema. Acceptable; the admin website is expected to refresh the schema via the event-info endpoint.

### Public surface

- The public event-info / availability response gains an `additionalDetails` array with `[{ key, name, maxLength, order }]`. Order is the array index.
- The public registration command/request gains `Dictionary<string, string> AdditionalDetails`.
- The admin registration view returns `additionalDetails` plus `historicalAdditionalDetails` (values whose keys are no longer in the current schema). The UI renders both, marking historical entries.

### Admin UI placement and UX

The schema editor is a new section on the existing registration policy page (matching the user's preference). It uses the same `react-hook-form`/zod patterns as other settings forms. Field rows show: `Name`, `Key` (read-only after creation; auto-generated from name on add), `MaxLength`, drag handle for reorder, remove button. A confirmation dialog appears when removing a field, explaining that historical values will be preserved.

### CLI parity

New CLI commands (subject to the broader ApiClient regeneration follow-up):
- `admitto event additional-details list <event>`
- `admitto event additional-details set <event> --from-json <file>` (atomic schema replacement)
- Existing `admitto registration get` already grows the new fields naturally via the regenerated client.

## Risks / Trade-offs

- **Risk**: JSONB storage hides schema drift — admins might accumulate orphaned historical keys forever. → **Mitigation**: Admin UI surfaces orphaned keys on the registration detail view; a future change can add an opt-in "purge orphan key" admin action. Tracked as out-of-scope for now.
- **Risk**: Allowing `MaxLength` to be reduced below existing stored values may surprise admins who later try to edit those records. → **Mitigation**: There is no edit path for additional details after registration today, so existing values are read-only; if such a path is added later, it must validate against the current `MaxLength`.
- **Risk**: Public event-info payload grows linearly with schema size. → **Mitigation**: A reasonable per-event cap (e.g., 25 fields) is enforced by the schema-update handler.
- **Trade-off**: No "required" semantics in the platform means the event website carries that responsibility. This is a deliberate product choice (per the originating request) and matches legacy behaviour after the simplification.
- **Trade-off**: Stable `Key` requires admins to learn a small extra concept. The UI auto-generates the key from the name on field creation to keep the common path one-click.
