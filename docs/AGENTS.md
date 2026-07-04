# Documentation Agent Guide

## Scope
This file applies to `/docs`.

## Primary Architecture Document
- Architecture documentation lives in `docs/arc42/` (one Markdown file per chapter).
- The index page is `docs/arc42/index.md`.
- ADRs live in `docs/adrs/` and are referenced from `docs/arc42/09-architectural-decisions.md`.

## Required Documentation Updates
- If code changes alter module structure, update `docs/arc42/05-building-block-view.md`.
- If runtime behavior changes, update `docs/arc42/06-runtime-view.md`.
- If deployment or operability changes, update `docs/arc42/07-deployment-view.md`.
- If cross-cutting patterns change, update `docs/arc42/08-crosscutting-concepts.md`.
- If this is an architectural decision, add/update an ADR in `docs/adrs/` and reference it from `docs/arc42/09-architectural-decisions.md`.

## Style Rules
- Prefer concrete conventions tied to code locations over abstract prose.
- Avoid duplicating architecture details across chapters; cross-reference instead.
- Record known gaps in `docs/arc42/11-risks-and-technical-debt.md`.
- Mark unknowns with `<!-- TODO: ... -->` rather than inventing content.
