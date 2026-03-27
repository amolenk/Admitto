# 11. Risks and technical debt

## 11.1 Known risks

| Risk | Impact | Likelihood | Mitigation |
| :--- | :----- | :--------- | :--------- |
| Module boundary erosion over time | High — coupling makes independent evolution impossible | Medium | Enforce via project structure, facades, and code review |
| Single PostgreSQL instance is a shared failure domain | High — all modules go down together | Low (managed DB) | Acceptable for target scale; schema isolation enables future split |

## 11.2 Technical debt

| Item | Impact | Priority | Notes |
| :--- | :----- | :------- | :---- |
| Worker host is stubbed | Outbox retry and scheduled jobs don't run | High | Background processing pipeline not yet implemented |
| Outbox orphan dispatch path is TODO | Messages could get stuck if best-effort dispatch fails and worker isn't running | High | Depends on worker implementation |
| Registration aggregate partially commented out | Registrations module is incomplete | Medium | Under active refactoring |
| Registrations infrastructure (DbContext) not implemented | No persistence for registrations | Medium | Blocked on domain stabilization |
| Admin UI not wired into AppHost | Local dev requires manual UI startup | Low | `cd src/Admitto.UI.Admin && pnpm dev` works standalone |
