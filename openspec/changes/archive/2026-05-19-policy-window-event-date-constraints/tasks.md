## 1. Domain — Registration Policy Guard

- [x] 1.1 Add `RegistrationWindowClosesAfterEventEnd` error to `TicketedEvent.Errors`
- [x] 1.2 In `TicketedEvent.ConfigureRegistrationPolicy`, assert `policy.ClosesAt <= this.EndsAt` and throw `BusinessRuleViolationException` on violation
- [x] 1.3 Write domain unit test: registration window closing exactly at `EndsAt` is accepted
- [x] 1.4 Write domain unit test: registration window closing after `EndsAt` is rejected

## 2. Domain — Reconfirm Policy Guard

- [x] 2.1 Add `ReconfirmWindowClosesAfterEventStart` error to `TicketedEvent.Errors`
- [x] 2.2 In `TicketedEvent.ConfigureReconfirmPolicy`, assert `policy != null → policy.ClosesAt < this.StartsAt` and throw `BusinessRuleViolationException` on violation
- [x] 2.3 Write domain unit test: reconfirm window closing one second before `StartsAt` is accepted
- [x] 2.4 Write domain unit test: reconfirm window closing exactly at `StartsAt` is rejected
- [x] 2.5 Write domain unit test: reconfirm window closing after `StartsAt` is rejected

## 3. API Integration Tests

- [x] 3.1 Add API test: setting registration window with `ClosesAt` after event `EndsAt` returns 400
- [x] 3.2 Add API test: setting reconfirm window with `ClosesAt` on or after event `StartsAt` returns 400

## 4. Admin UI — Registration Policy Form

- [x] 4.1 In the registration policy form, add client-side validation that `closesAt <= event.endsAt` (event dates are already available from the loaded event query)
- [x] 4.2 Display a clear error message when the registration window close date exceeds the event end date

## 5. Admin UI — Reconfirm Policy Form

- [x] 5.1 In the reconfirm policy form, add client-side validation that `closesAt < event.startsAt` (event dates are already available from the loaded event query)
- [x] 5.2 Display a clear error message when the reconfirm window close date is on or after the event start date
