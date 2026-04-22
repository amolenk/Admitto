using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Registration;

public class ConfigureRegistrationPolicySettings : TeamEventSettings
{
    [CommandOption("--window-open")]
    [Description("The registration window open date/time (ISO 8601). Required.")]
    public string? WindowOpensAt { get; set; }

    [CommandOption("--window-close")]
    [Description("The registration window close date/time (ISO 8601). Required.")]
    public string? WindowClosesAt { get; set; }

    [CommandOption("--allowed-domain")]
    [Description("Restrict self-service registration to this email domain (e.g. acme.com). Omit to remove restriction.")]
    public string? AllowedEmailDomain { get; set; }

    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the event (optimistic concurrency token).")]
    public int? ExpectedVersion { get; set; }
}

public class ConfigureRegistrationPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ConfigureRegistrationPolicySettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ConfigureRegistrationPolicySettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        if (string.IsNullOrWhiteSpace(settings.WindowOpensAt) ||
            !DateTimeOffset.TryParse(settings.WindowOpensAt, out var opensAt))
        {
            AnsiConsoleExt.WriteErrorMessage("--window-open is required and must be an ISO 8601 date/time.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(settings.WindowClosesAt) ||
            !DateTimeOffset.TryParse(settings.WindowClosesAt, out var closesAt))
        {
            AnsiConsoleExt.WriteErrorMessage("--window-close is required and must be an ISO 8601 date/time.");
            return 1;
        }

        var request = new ConfigureRegistrationPolicyHttpRequest
        {
            OpensAt = opensAt,
            ClosesAt = closesAt,
            AllowedEmailDomain = settings.AllowedEmailDomain,
            ExpectedVersion = settings.ExpectedVersion
        };

        var result = await admittoService.SendAsync(client =>
            client.ConfigureRegistrationPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully configured registration policy.");
        return 0;
    }
}
