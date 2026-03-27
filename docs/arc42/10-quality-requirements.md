# 10. Quality requirements

Quality goals from [chapter 1](01-introduction-and-goals.md) are made concrete here.

## 10.1 Quality scenarios

| ID | Quality | Stimulus | Response | Metric / target | Chapter 1 goal |
| :- | :------ | :------- | :------- | :-------------- | :------------- |
| Q-01 | Maintainability | Developer adds a new use case to a module | Change is contained within the module's project | Zero cross-module code changes | #1 |
| Q-02 | Reliability | Concurrent registrations for the last ticket | One succeeds, one gets a conflict error | Optimistic concurrency enforced via `Version` column | #2 |
| Q-03 | Reliability | Outbox dispatch fails after transaction commit | Message stays in outbox table for background retry | No messages lost | #2 |
| Q-04 | Security | Unauthenticated request to admin endpoint | 401 returned before handler executes | JWT validation runs in middleware | #4 |
| Q-05 | Operational simplicity | Operator deploys a new version | Single build artifact per host; no service mesh or discovery required | One container image per host, shared database and queue | #3 |

## 10.2 Test strategy

| Layer | What's tested | Project | Approach |
| :---- | :------------ | :------ | :------- |
| Domain + Application | Aggregate invariants, value objects, handlers, facades | `Admitto.Module.*.Tests` | Unit tests (builder pattern) and Aspire-backed integration tests |
| End-to-end | Full HTTP request → response | `Admitto.Api.Tests` | Integration tests with JWT token generation, Respawn for DB cleanup |

Test helpers live in `Admitto.Testing` (Aspire fixtures, Respawn, Shouldly) and `Admitto.TestHelpers`.

## Done-when

- [x] Each quality goal from chapter 1 has at least one scenario.
- [x] Each scenario has a measurable metric or acceptance criterion.
- [x] Scenarios link back to the relevant chapter 1 quality goal.
