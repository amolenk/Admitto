# Admitto CLI — Agent Guide

## Scope

This file applies to `src/Admitto.Cli/` and all CLI command implementation.

## Architecture

The CLI is a **thin HTTP client** — all business logic lives in the API backend. Commands are lightweight wrappers around the NSwag-generated `ApiClient`.

```
User Input → Spectre.Console.Cli → IAdmittoService → ApiClient (NSwag) → REST API
```

## File layout

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

## Command patterns

### Settings classes

Commands declare settings (CLI arguments/options) as a class extending `CommandSettings`:

- **`TeamSettings`** — base for team-scoped commands; provides `-t|--team` option.
- **`TeamEventSettings`** — extends `TeamSettings` with `-e|--event` option.
- Custom settings classes for commands with additional parameters; override `Validate()` for required fields.

### Mutation commands

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

### Query commands

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

## Registering commands in Program.cs

Add command branches in `Program.cs` using `config.AddBranch()`:

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

## NSwag API client

`ApiClient.g.cs` is auto-generated from the API's OpenAPI spec. After adding new API endpoints:

1. Run the API to generate the updated OpenAPI spec.
2. Regenerate the client: `./generate-api-client.sh` (or `dotnet tool run nswag run nswag.json`).
3. The new methods become available on the `ApiClient` class.

**Do not edit `ApiClient.g.cs` manually.**

## Conventions

- Return `0` on success, `1` on failure.
- Use `InputHelper.ResolveTeamSlug/ResolveEventSlug` — never access `configService` defaults directly.
- Use `AnsiConsoleExt.WriteSuccessMessage()` for success feedback.
- Error handling is automatic via `IAdmittoService` wrappers — do not add try/catch in commands.
- One file per command (Settings + Command class in the same file unless settings are shared).
