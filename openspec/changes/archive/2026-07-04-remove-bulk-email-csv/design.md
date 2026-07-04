## Context

Bulk email currently supports two recipient sources modeled as a polymorphic value object `BulkEmailJobSource`: `AttendeeSource` (a `QueryRegistrationsDto` filter, resolved live from Registrations) and `ExternalListSource` (a literal `(Email, DisplayName?)` list — the CSV path). Removing the CSV/external-list capability (see `proposal.md`) leaves exactly one source shape. The database has **no `BulkEmailJob` rows**, so we are free to change persisted shapes without migrating data.

The proposal calls this out as an opportunity to "collapse" the now-single-source model. This design evaluates each collapse candidate and decides, per piece, whether to **collapse** (keep the concept, simplify its shape) or **remove entirely** (delete the type/field/branch).

## Goals / Non-Goals

**Goals:**
- Remove the external-list source end-to-end (domain, application, HTTP, persistence, UI, CLI spec, tests).
- Aggressively delete now-dead abstractions rather than leaving single-case shells behind.
- Give the Email module ownership of its persisted recipient-filter shape, decoupled from the `Registrations.Contracts` query DTO.
- Keep the `BulkEmailJob` lifecycle, snapshot/freeze, fan-out, cancellation, and audit behavior unchanged.

**Non-Goals:**
- Duplicating the `RegistrationStatus` contract enum inside Email. We reuse sanctioned `Registrations.Contracts` primitives (`RegistrationStatus`, `RegistrationId`) — Email's domain already depends on them. The decoupling target is the persisted *DTO shape*, not shared contract primitives.
- Changing anything about reconfirm/system-triggered bulk jobs other than the mechanical source-shape update.
- Data migration (table is empty).

## Decisions

The core question the proposal raises — "do we collapse or remove entire fields?" — resolved per element:

### D1. `BulkEmailJobSource` polymorphic VO → **REMOVE the hierarchy; persist an Email-owned filter VO**

The abstract `BulkEmailJobSource` + `AttendeeSource` + `[JsonPolymorphic]`/`$type` discriminator exist only to distinguish two cases. With one case they carry zero information.

- **Decision:** Delete `BulkEmailJobSource`, `AttendeeSource`, and `ExternalListSource`. Replace the job's `Source` property with an **Email-module-owned** `BulkEmailAttendeeFilter` value object (in `Email/Domain/ValueObjects`), persisted as `jsonb`, no discriminator.
- **Why not persist `QueryRegistrationsDto` directly?** That is a `Registrations.Contracts` type. Persisting it as Email's durable aggregate state couples Email's on-disk schema to a DTO another module owns and may evolve for query reasons unrelated to bulk email. `BulkEmailAttendeeFilter` gives Email ownership of its persisted shape; the contract DTO is constructed only transiently at the facade-call boundary (see D1a).
- **Why not collapse to a single-case record** wrapping the filter? That keeps a wrapper with no discriminating value. The filter *is* the source now — persist it plainly as `AttendeeFilter`.
- **Field set:** mirror the fields Email actually uses — `TicketTypeIds?`, `RegistrationStatus?`, `HasReconfirmed?`, `RegisteredAfter?`, `RegisteredBefore?`, `AdditionalDetailEquals?`, `RegistrationIds?`. It reuses the contract primitive `RegistrationStatus` enum (see Non-Goals) rather than duplicating it.
- **Rename:** property `Source` → `AttendeeFilter`, column `source` → `attendee_filter`. Cheap because the table is empty, and it stops implying a variant type still exists.

### D1a. Contract mapping location → **Application layer, at the facade boundary**

The `BulkEmailAttendeeFilter → QueryRegistrationsDto` mapping happens in exactly one place: `BulkEmailRecipientResolver` (Application), immediately before calling `IRegistrationsFacade.GetRegistrationsAsync`. The domain VO stays free of query-DTO construction; the resolver already owns the cross-module contract call, so translating persisted Email state into the contract shape there keeps the boundary crisp. Both producers of the filter (HTTP create + reconfirm job) now construct the Email-owned `BulkEmailAttendeeFilter`, never the contract DTO.

### D2. `ExternalListSource` / `ExternalListItem` → **REMOVE**

No consumers remain. Delete both types and the resolver's `ResolveExternalList` branch and the `default => throw` arm (the `switch` collapses to a single straight-line call).

### D3. `BulkEmailRecipient.RegistrationId?` → **REMOVE the nullability (make it required)**

`RegistrationId` was nullable *only* to accommodate external-list recipients. Every remaining recipient originates from a registration.

- **Decision:** Make `RegistrationId` non-nullable on `BulkEmailRecipient`.
- **Ripple — fan-out link building:** `BuildRegistrationLink(publicEventLink, action, RegistrationId? registrationId)` currently falls back to the bare public event link when the id is null (`SendBulkEmailJob.cs:357-360`). With a guaranteed id, **remove the null-fallback branch** and always build the per-registration link. The `RegistrationId?` parameter becomes non-nullable and the branch disappears.
- **Persistence:** `registration_id` lives inside the `recipients` `jsonb` (owned `ToJson` collection), so requiring it is a C#-nullability change, not a table-schema constraint. Mark the JSON property `IsRequired()`.

### D4. `BulkEmailRecipient.DisplayName?` → **REMOVE the nullability (make it required)**

Correction from an earlier reading: `DisplayName` nullability is *also* an external-list artifact, not intrinsic to attendees. `FirstName` and `LastName` are Vogen value objects that reject null/whitespace and trim (`FirstName.cs:12-19`, `LastName.cs:12-19`), so for an attendee `displayName = (FirstName + " " + LastName).Trim()` is **always non-empty**. The resolver's `string.IsNullOrWhiteSpace(displayName) ? null : displayName` guard (`BulkEmailRecipientResolver.cs:65-69`) can never yield `null` for attendees — it only existed because `ExternalListItem.DisplayName` was optional.

- **Decision:** Make `DisplayName` non-nullable on `BulkEmailRecipient`. Simplify the resolver to assign the concatenated name directly (drop the whitespace-to-null guard).
- **Ripple — fan-out recipient name:** `RecipientName: recipient.DisplayName ?? recipient.Email.Value` (`SendBulkEmailJob.cs:252`) — **remove the `?? Email` fallback**; the display name is always present.
- **Read-side DTOs:** `PreviewBulkEmailResponse` / `BulkEmailJobDetailDto` expose `string? DisplayName`. Make these non-nullable too for consistency (widening, harmless) — a minor SDK type change.
- **Persistence:** mark the recipients `display_name` JSON property `IsRequired()`.

### D5. HTTP contract `BulkEmailSourceHttpDto` wrapper → **REMOVE the wrapper; request carries the attendee filter directly**

Today `source` is a one-of envelope (`{ attendee?, externalList? }`). With one source, the envelope is noise.

- **Decision:** Delete `BulkEmailSourceHttpDto`, `ExternalListSourceHttpDto`, `ExternalListRecipientHttpDto`. The create/preview request exposes the attendee filter directly (e.g. `AttendeeFilterHttpDto` with the existing filter fields), mapped straight to `QueryRegistrationsDto`.
- **Validation:** delete the entire custom "exactly one of attendee/externalList" rule and the empty-external-list rule (`CreateBulkEmailValidator.cs:25-45`). Keep only `filter NotNull` plus the existing subject/body rules.
- **BREAKING** contract change → regenerate the Admin UI SDK before touching proxy/UI code.

### D6. `IBulkEmailRecipientResolver` signature → **COLLAPSE (keep the service, simplify)**

The resolver is still needed (it queries the facade and builds the snapshot). Its input changes to the Email-owned filter: `ResolveAsync(..., BulkEmailJobSource source, ...)` → `ResolveAsync(..., BulkEmailAttendeeFilter attendeeFilter, ...)`. Inside, it maps the Email VO to `QueryRegistrationsDto` (D1a) and calls the facade. Delete the `switch`, the external-list method, and the doc reference to two shapes. This is a collapse, not a removal — the abstraction earns its keep and now also owns the contract mapping.

### D7. EF migration → **VERIFY, expect near-no-op**

The `source`/`attendee_filter` value stays a `jsonb` column (only its serialized shape and column name change) and `registration_id` stays inside the recipients `jsonb`. The only real schema delta is the **column rename** `source → attendee_filter`. Since the table is empty, a rename (or drop+add) migration is trivial and loses nothing. Generate the migration via the `ef-migrations` skill/tooling; do not hand-edit.

### Summary: collapse vs remove

| Element | Verdict |
|---|---|
| `BulkEmailJobSource` hierarchy + `$type` | **Remove** — persist Email-owned `BulkEmailAttendeeFilter` |
| `AttendeeSource` wrapper | **Remove** (folded into the job property) |
| Persisting `QueryRegistrationsDto` in Email's DB | **Remove** — map to it only transiently at the facade call |
| `ExternalListSource` / `ExternalListItem` | **Remove** |
| `BulkEmailRecipient.RegistrationId?` | **Remove nullability** → required |
| Fan-out public-link fallback branch | **Remove** |
| `BulkEmailRecipient.DisplayName?` | **Remove nullability** → required (drop resolver null-guard + `?? Email` fallback) |
| `BulkEmailSourceHttpDto` + external-list DTOs | **Remove** |
| "exactly one source" + empty-list validation | **Remove** |
| `IBulkEmailRecipientResolver` | **Collapse** signature; keep service |
| `source` jsonb column | **Rename** to `attendee_filter`; shape simplified |

## Risks / Trade-offs

- **[Persisted JSON shape changes without a data migration]** → Acceptable: the table is verified empty. If any environment somehow has rows, they would fail to deserialize — mitigate by confirming emptiness before deploy (the migration for the empty table makes this explicit).
- **[Non-nullable `RegistrationId` is a latent invariant]** → All current producers (attendee resolver, reconfirm job) supply a real id, so the invariant holds. Enforced at the type level going forward, so future non-attendee sources would be a compile-time conversation, not a silent null.
- **[BREAKING HTTP contract]** → Internal Admin UI + CLI-spec only (no public/external consumers). Mitigated by regenerating the SDK and updating the send sheet in the same change.
- **[Coupling to `QueryRegistrationsDto` in Email persistence]** → Eliminated by D1: Email persists its own `BulkEmailAttendeeFilter` and maps to the contract DTO only at the facade boundary. Residual coupling is limited to the shared contract primitive `RegistrationStatus` (by design).
- **[`BulkEmailAttendeeFilter` drifting from `QueryRegistrationsDto`]** → The single mapper in `BulkEmailRecipientResolver` is the only bridge; a new filter field is a compile-time change in one place. Covered by resolver tests.

## Migration Plan

1. Domain/application/HTTP/persistence changes in `Admitto.Core` (Email module): add `BulkEmailAttendeeFilter`, remove the source hierarchy, add the filter→`QueryRegistrationsDto` mapper in the resolver, and update the reconfirm job to construct `BulkEmailAttendeeFilter`.
2. Generate the EF migration with official tooling (column rename); no data backfill.
3. Regenerate the Admin UI SDK from the updated spec, then remove CSV logic from the send sheet.
4. Update `bulk-email`, `admin-ui-bulk-emails`, `cli-admin-parity` specs and ADR-009 / arc42 notes.
5. Update/remove affected tests (resolver external-list cases, create/preview both-sources cases, domain tests).

**Rollback:** revert the change; since the table is empty there is no data to reconcile.

## Open Questions

- None. (The only prior unknown — historical `external_list` rows — is closed by the empty-table fact.)
