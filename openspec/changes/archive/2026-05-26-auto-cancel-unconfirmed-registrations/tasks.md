## 1. Revert — TicketedEventReconfirmPolicy (v1 changes)

- [x] 1.1 Remove `AutoCancelEnabled` and `MaxReconfirmAttempts` from `TicketedEventReconfirmPolicy` value object and its `Create(...)` method
- [x] 1.2 Remove the validation rule that requires `MaxReconfirmAttempts` when `AutoCancelEnabled=true`
- [x] 1.3 Update `TicketedEvent.SetReconfirmPolicy(...)` to remove the two reverted parameters
- [x] 1.4 Update domain-level unit tests to remove auto-cancel policy scenarios (keep `Cancel(ReconfirmAutoCancel)` test)
- [x] 1.5 Remove `AutoCancelEnabled` and `MaxReconfirmAttempts` from `TicketedEventReconfirmPolicySnapshot` in `TicketedEventReconfirmPolicyChangedIntegrationEvent`
- [x] 1.6 Remove `AutoCancelEnabled` and `MaxReconfirmAttempts` from `ReconfirmTriggerSpecDto`
- [x] 1.7 Remove `AutoCancelEnabled` and `MaxReconfirmAttempts` from `ConfigureReconfirmPolicyCommand`, handler, `ConfigureReconfirmPolicyHttpRequest`, validator, and response DTO
- [x] 1.8 Remove `AutoCancelEnabled` and `MaxReconfirmAttempts` from `RegistrationsIntegrationEventPublisher` mapping
- [x] 1.9 Update API-level tests to remove auto-cancel validation test cases
- [x] 1.10 Revert the EF Core migration that added `auto_cancel_enabled` and `max_reconfirm_attempts` to reconfirm policy storage

## 2. Revert — Email Module Trigger Job Data (v1 changes)

- [x] 2.1 Remove `AutoCancelEnabledKey` and `MaxReconfirmAttemptsKey` constants from `RequestReconfirmationsJob`
- [x] 2.2 Remove the reading of `AutoCancelEnabled`/`MaxReconfirmAttempts` from trigger job data in `RequestReconfirmationsJob.Execute`
- [x] 2.3 Remove writing of `AutoCancelEnabled`/`MaxReconfirmAttempts` into trigger job data from `ScheduleReconfirmationsHandler`
- [x] 2.4 Update trigger/scheduling tests to remove assertions on those job data keys

## 3. Revert — Admin UI Reconfirmation Policy Form (v1 changes)

- [x] 3.1 Remove `autoCancelEnabled` and `maxReconfirmAttempts` fields from the Zod schema in `reconfirm-policy-form.tsx`
- [x] 3.2 Remove the auto-cancel toggle (`Switch`) and max-attempts number input from the form JSX
- [x] 3.3 Remove `autoCancelEnabled` and `maxReconfirmAttempts` from the form default values and the submit payload
- [x] 3.4 Remove `autoCancelEnabled` and `maxReconfirmAttempts` from the `ReconfirmPolicy` interface in `event-detail-types.ts`

## 4. Domain — Add `MaxReconfirmAttempts` to `TicketType`

- [x] 4.1 Add `MaxReconfirmAttempts: int?` property to `TicketType` entity with an `UpdateMaxReconfirmAttempts(int? value)` method
- [x] 4.2 Add validation: when set, `MaxReconfirmAttempts` must be ≥ 1
- [x] 4.3 Add domain-level unit tests: set valid value → ok; set 0 → error; set null → ok (disables auto-cancel for this type)

## 5. Infrastructure — Database Migration

- [x] 5.1 Persist `max_reconfirm_attempts` in ticket type storage via the owned JSON mapping on `TicketCatalog` (no separate relational column migration is required in this model)
- [x] 5.2 Verify the persistence change is non-destructive (existing ticket types default to null)

## 6. Contracts — Extend `RegistrationListItemDto` with `EffectiveMaxReconfirmAttempts`

- [x] 6.1 Add `EffectiveMaxReconfirmAttempts: int?` to `RegistrationListItemDto` in `Registrations.Contracts` namespace
- [x] 6.2 Update `IRegistrationsFacade` implementation (`QueryRegistrationsAsync`) to compute `EffectiveMaxReconfirmAttempts` per registration: JOIN registration's ticket type IDs against live `TicketType.MaxReconfirmAttempts`, take MIN of non-null values (null if none set)
- [x] 6.3 Update all callers/tests of `RegistrationListItemDto` to handle the new nullable field (provide `null` in builders/fixtures)

## 7. Email Module — Use Per-Registration Threshold in `RequestReconfirmationsJob`

- [x] 7.1 Update the tick to read `EffectiveMaxReconfirmAttempts` from each `RegistrationListItemDto` instead of from trigger job data
- [x] 7.2 Update candidate split logic: reconfirm set = `EffectiveMaxReconfirmAttempts == null || email_log_count < EffectiveMaxReconfirmAttempts`; auto-cancel set = `EffectiveMaxReconfirmAttempts != null && email_log_count >= EffectiveMaxReconfirmAttempts`
- [x] 7.3 Update job tests: mixed ticket types (some with, some without `MaxReconfirmAttempts`) — correct split; session-only attendee never auto-cancelled; workshop attendee at threshold is auto-cancelled

## 8. Backend API — Ticket Type Endpoints

- [x] 8.1 Extend `AddTicketTypeCommand` / `UpdateTicketTypeCommand` to include `MaxReconfirmAttempts: int?`
- [x] 8.2 Extend corresponding HTTP request DTOs and FluentValidation validators (MaxReconfirmAttempts must be ≥ 1 when provided)
- [x] 8.3 Update command handlers to pass `MaxReconfirmAttempts` through to the domain
- [x] 8.4 Extend ticket type response DTOs to include `MaxReconfirmAttempts`
- [x] 8.5 Add API-level tests: set valid value → saved; set 0 → validation error; omit → null

## 9. Admin UI — Ticket Type Form

- [x] 9.1 Regenerate Admin UI SDK (`aspire start --isolated` → `aspire wait api` → `curl spec` → `pnpm openapi-ts`)
- [x] 9.2 Add optional "Max reconfirmation attempts" number input to the Add Ticket Type form with hint explaining auto-cancel behaviour
- [x] 9.3 Add the same field to the Edit Ticket Type form (if separate), pre-filled from existing value
- [x] 9.4 Add client-side validation: when provided, must be a positive integer ≥ 1
- [x] 9.5 Wire the field into the submit payload; pass `null` when left empty
