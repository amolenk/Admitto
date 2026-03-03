# Documentation Agent Guide

## Scope
This file applies to `/docs`.

## Primary Architecture Document
- `/docs/README.md` is the canonical architecture document (arc42 style).
- Keep section numbering stable (`1` through `12`) so references from other docs and automation remain valid.

## Required Documentation Updates
- If code changes alter architecture patterns, update `/docs/README.md` section `5.3`.
- If runtime behavior changes, update `/docs/README.md` section `6`.
- If deployment or operability changes, update `/docs/README.md` sections `7` and `8`.
- If this is an architectural decision, add/update an ADR in `/docs/adrs` and reference it from section `9`.

## Style Rules
- Prefer concrete conventions tied to code locations over abstract prose.
- Avoid duplicating architecture details in multiple files; link back to `/docs/README.md` where possible.
- Record known gaps/TODO architecture work in section `11 Risks and Technical Debt`.
