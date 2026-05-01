# Admitto CLI — Agent Guide

## Scope
This file applies to `src/Admitto.Cli/`.

## Status: Legacy Project
**This project is now considered legacy.** 
- No further changes will be made to the CLI regardless of future breakage
- All business logic lives in the API backend
- The CLI is a thin HTTP client that wraps the NSwag-generated ApiClient

## Architecture
The CLI is a **thin HTTP client** — all business logic lives in the API backend. Commands are lightweight wrappers around the NSwag-generated `ApiClient`.

```
User Input → Spectre.Console.Cli → IAdmittoService → ApiClient (NSwag) → REST API
```

## File Layout

```
src/Admitto.Cli/
├── Commands/
│   └── {Feature}/              # One folder per feature domain
│       ├── {Action}Command.cs  # Command class + Settings class
│       └── ...
├── Api/
│   ├── AdmittoService.cs       # IAdmittoService — wraps ApiClient with auth + error handling
│   └── ApiClient.g.cs          # NSwag-generated — DO NOT edit manually
├── IO/
│   ├── InputHelper.cs          # Slug resolution and argument parsing
│   └── AnsiConsoleExt.cs       # Styled console output helpers
└── Program.cs                  # Command tree registration via Spectre.Console.Cli
```

## Command Patterns

### Settings Classes
- **`TeamSettings`** — base for team-scoped commands; provides `-t|--team`.
- **`TeamEventSettings`** — extends `TeamSettings` with `-e|--event`.
- Custom settings classes for additional parameters; override `Validate()` for required fields.

### Mutation Commands
Use `IAdmittoService.SendAsync()` for write operations:

```csharp
public class CreateCouponCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<CreateCouponSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, CreateCouponSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new CreateCouponRequest { /* map from settings */ };
        var success = await admittoService.SendAsync(
            client => client.CreateCouponAsync(teamSlug, eventSlug, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccessMessage("Coupon created.");
        return 0;
    }
}
```

### Query Commands
Use `IAdmittoService.QueryAsync()` for read operations. Format results as a Spectre.Console `Table`:

```csharp
public class ListCouponsCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(
            client => client.GetCouponsAsync(teamSlug, eventSlug, cancellationToken));
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email");
        table.AddColumn("Status");
        // ... add rows ...

        AnsiConsole.Write(table);
        return 0;
    }
}
```

## Registering Commands in Program.cs

```csharp
config.AddBranch("coupon", coupon =>
{
    coupon.SetDescription("Manage coupons");
    coupon.AddCommand<CreateCouponCommand>("create").WithDescription("Create a coupon");
    coupon.AddCommand<ListCouponsCommand>("list").WithDescription("List coupons for an event");
    coupon.AddCommand<ShowCouponCommand>("show").WithDescription("Show coupon details");
    coupon.AddCommand<RevokeCouponCommand>("revoke").WithDescription("Revoke a coupon");
});
```

## NSwag API Client
`ApiClient.g.cs` is auto-generated. **Do not edit it manually.** After adding new API endpoints, regenerate via the `cli-api-client-generation` skill or by running `./generate-api-client.sh`.

Do not add handwritten `ApiClient` partials as a substitute for regeneration when a new endpoint is missing. If the generated client cannot be refreshed, treat that as an environment or AppHost/spec-availability problem and fix that first.

In this repository, the reliable path is:
1. `aspire start --isolated`
2. `aspire wait api`
3. confirm the live spec is reachable at `/openapi/v1.json` on the `api` endpoint from `aspire describe`
4. regenerate the CLI client

## Quarantining Commands When the API Surface Shrinks
When the backend removes endpoints, regenerating `ApiClient.g.cs` can break existing commands. **Do not delete commands** — quarantine them instead.

Watch for: a failure in a shared file (e.g. `IO/InputHelper.cs`) suppresses downstream errors; fixing the shared file surfaces the full cascade.

How to quarantine:
1. Exclude affected files via a labeled `<ItemGroup>` in `Admitto.Cli.csproj`:
   ```xml
   <ItemGroup Label="Quarantined: backend admin endpoints removed; restore once the API exposes ... again.">
     <Compile Remove="Commands/Attendee/**/*.cs" />
   </ItemGroup>
   ```
2. Comment out (don't delete) the corresponding `AddCommand` / `AddBranch` registrations in `Program.cs`.
3. Comment out any `IAdmittoService` helper methods used only by quarantined commands.
4. Build until clean; prefer fixing surviving files over expanding the quarantine.

Restoration: regenerate `ApiClient.g.cs`, remove `<Compile Remove>` entries, uncomment `Program.cs` registrations, rebuild.

## Conventions
- Return `0` on success, `1` on failure.
- Use `InputHelper.ResolveTeamSlug/ResolveEventSlug` — never access `configService` defaults directly.
- Use `AnsiConsoleExt.WriteSuccessMessage()` for success feedback.
- Error handling is automatic via `IAdmittoService` — do not add try/catch in commands.
- One file per command (Settings + Command class together unless settings are shared).
