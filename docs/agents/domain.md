# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`docs/arc42/`**: the source of truth for Admitto's architecture, constraints, decisions, and concepts. Read the chapters relevant to the work.
- **`CONTEXT.md`** at the repo root, if it exists.
- **`docs/adr/`**: read ADRs that touch the area you're about to work in.

If `CONTEXT.md` does not exist, proceed silently. Don't flag its absence or suggest creating it upfront. The `/domain-modeling` skill creates it lazily when terms or decisions actually get resolved.

## File structure

Single-context repo:

```text
/
├── CONTEXT.md
├── docs/
│   ├── arc42/
│   └── adr/
│       ├── adr-001-modular-monolith.md
│       └── ...
└── src/
```

## Use the glossary's vocabulary

When output names a domain concept, use the term defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept isn't in the glossary, reconsider whether the project already has a term; otherwise, note the gap for `/domain-modeling`.

## Flag ADR conflicts

If an output contradicts an existing ADR, surface it explicitly rather than silently overriding it.
