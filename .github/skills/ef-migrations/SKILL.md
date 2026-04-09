---
name: ef-migrations
description: Generate EF Core migrations with the official tooling and prevent manual edits to migration files.
---

# Skill: ef-migrations

## Purpose

Generate and validate EF Core migrations through the approved tooling workflow when backend persistence changes require schema updates.

## Use when

- a backend feature changes EF Core entity configuration, DbContext mappings, or schema shape
- a new migration is required in `src/*/Migrations/`
- an existing migration needs to be superseded by a newly generated migration rather than hand-edited

## Inputs

- the feature spec and affected backend scope
- the target DbContext and migrations project
- the current migration history in the affected project
- the relevant architecture and module constraints

## Required workflow

1. Identify the correct DbContext, startup project, and migrations output location.
2. Use EF Core tooling to generate the migration.
3. Review the generated migration and model snapshot for correctness.
4. If the generated result is wrong, fix the model/configuration and generate again.
5. Build the affected projects.
6. If the backend surface is runnable, use the `aspire` skill to start or refresh the AppHost, wait for the relevant resources, and inspect health before declaring the migration work complete.

## Must validate

- the migration was produced by EF Core tooling rather than manual editing
- the generated migration matches the intended schema change
- the generated snapshot is consistent with the model
- the affected projects still compile

## Must not do

- edit migration files manually
- patch the model snapshot manually as a substitute for regeneration
- leave the repo with partially generated migration artifacts
- skip health verification when the changed backend surface is runnable

## Outputs

- EF-generated migration files and snapshot updates
- a brief record of which DbContext and tooling command were used
- a note for reviewers when no migration was needed after validation

## Override points

- exact `dotnet ef` command arguments
- target project and startup project
- migration naming convention already used by the affected module

## Examples

- Adding a column to a module-owned table after updating the entity configuration.
- Creating a new migration for a feature that introduces a new aggregate persistence model.
