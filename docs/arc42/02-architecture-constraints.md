# 2. Architecture constraints

## Technical constraints

| Constraint | Rationale |
| :--------- | :-------- |
| .NET and C# | The team's primary technology stack; all hosts, modules, and shared infrastructure are built on it |

Technology choices such as PostgreSQL, EF Core, and Azure Storage Queues are documented as architectural decisions in [Chapter 9](09-architectural-decisions.md), not as constraints — they were chosen by the team and could be revisited.

## Done-when

- [x] Every item in this chapter is genuinely externally imposed or non-negotiable.
- [x] Technology choices are captured as ADRs, not listed here.
- [ ] Organizational constraints (team size, budget, timeline) are documented if relevant.
