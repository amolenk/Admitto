using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Registration;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--window-open")]
    [Description("The registration window open date/time (ISO 8601)")]
    public string? WindowOpensAt { get; set; }

    [CommandOption("--window-close")]
    [Description("The registration window close date/time (ISO 8601)")]
    public string? WindowClosesAt { get; set; }

    [CommandOption("--allowed-domain")]
    [Description("Restrict self-service registration to this email domain (e.g. @acme.com). Omit to remove restriction.")]
    public string? AllowedEmailDomain { get; set; }
}

public class SetRegistrationPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SetSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        DateTimeOffset? windowOpensAt = null;
        if (!string.IsNullOrWhiteSpace(settings.WindowOpensAt))
        {
            if (!DateTimeOffset.TryParse(settings.WindowOpensAt, out var parsed))
            {
                AnsiConsoleExt.WriteErrorMessage("Invalid --window-open value. Expected ISO 8601 date/time.");
                return 1;
            }

            windowOpensAt = parsed;
        }

        DateTimeOffset? windowClosesAt = null;
        if (!string.IsNullOrWhiteSpace(settings.WindowClosesAt))
        {
            if (!DateTimeOffset.TryParse(settings.WindowClosesAt, out var parsed))
            {
                AnsiConsoleExt.WriteErrorMessage("Invalid --window-close value. Expected ISO 8601 date/time.");
                return 1;
            }

            windowClosesAt = parsed;
        }

        var request = new SetRegistrationPolicyHttpRequest
        {
            RegistrationWindowOpensAt = windowOpensAt,
            RegistrationWindowClosesAt = windowClosesAt,
            AllowedEmailDomain = settings.AllowedEmailDomain
        };

        var result = await admittoService.SendAsync(client =>
            client.SetRegistrationPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully set registration policy.");
        return 0;
    }
}
