---
name: cli-api-client-generation
description: Refresh the Admitto CLI NSwag ApiClient through the approved Aspire-backed generation workflow.
---

# Skill: cli-api-client-generation

## Purpose

Update the Admitto CLI generated API client through the approved workflow whenever backend endpoint changes affect `src/Admitto.Cli/Api/ApiClient.g.cs`.

## Use when

- backend endpoint additions, removals, or signature changes affect the CLI API surface
- `src/Admitto.Cli/Api/ApiClient.g.cs` must be refreshed
- a CLI command depends on newly generated client methods

## Inputs

- the changed backend endpoint surface
- the CLI project at `src/Admitto.Cli`
- the generation script at `src/Admitto.Cli/generate-api-client.sh`
- the existing Aspire AppHost configuration

## Required workflow

1. Use the `aspire` skill to start or refresh the AppHost.
2. Wait for the API and other relevant resources to become healthy.
3. Confirm the backend surface needed for OpenAPI generation is available.
4. Run [`src/Admitto.Cli/generate-api-client.sh`](/Users/amolenk/Code/amolenk/Admitto/src/Admitto.Cli/generate-api-client.sh).
5. Review the generated client diff.
6. Build the CLI project.
7. Inspect Aspire resource health before declaring the generation pass complete.

## Must validate

- `ApiClient.g.cs` changed only through the generation script
- the generated client matches the current backend contract
- the CLI project compiles after regeneration
- relevant Aspire resources are healthy

## Must not do

- edit `src/Admitto.Cli/Api/ApiClient.g.cs` manually
- bypass the generation script with ad hoc manual code changes
- assume the API is healthy without checking Aspire resource status
- finish the workflow without a CLI build

## Outputs

- updated generated CLI client produced by `generate-api-client.sh`
- a brief record that Aspire was started or refreshed and resources were checked
- a note for reviewers when no client regeneration was required

## Override points

- the exact Aspire resource names to wait for
- any additional CLI verification command beyond the basic build

## Examples

- Regenerating the CLI client after adding a new admin endpoint.
- Refreshing generated methods after a request or response contract change.
