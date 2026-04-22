using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class ConfigureReconfirmPolicySettings : TeamEventSettings
{
    [CommandOption("--opens-at")]
    [Description("Reconfirm window open date/time (ISO 8601). Omit to clear.")]
    public string? OpensAt { get; set; }

    [CommandOption("--closes-at")]
    [Description("Reconfirm window close date/time (ISO 8601). Omit to clear.")]
    public string? ClosesAt { get; set; }

    [CommandOption("--cadence-days")]
    [Description("Number of days between reconfirm reminders. Omit to clear.")]
    public int? CadenceDays { get; set; }

    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the event (optimistic concurrency token).")]
    public int? ExpectedVersion { get; set; }
}

public class ConfigureReconfirmPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ConfigureReconfirmPolicySettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ConfigureReconfirmPolicySettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        DateTimeOffset? opensAt = null;
        if (!string.IsNullOrWhiteSpace(settings.OpensAt))
        {
            if (!DateTimeOffset.TryParse(settings.OpensAt, out var parsed))
            {
                AnsiConsoleExt.WriteErrorMessage("Invalid --opens-at value. Expected ISO 8601 date/time.");
                return 1;
            }

            opensAt = parsed;
        }

        DateTimeOffset? closesAt = null;
        if (!string.IsNullOrWhiteSpace(settings.ClosesAt))
        {
            if (!DateTimeOffset.TryParse(settings.ClosesAt, out var parsed))
            {
                AnsiConsoleExt.WriteErrorMessage("Invalid --closes-at value. Expected ISO 8601 date/time.");
                return 1;
            }

            closesAt = parsed;
        }

        var request = new ConfigureReconfirmPolicyHttpRequest
        {
            OpensAt = opensAt,
            ClosesAt = closesAt,
            CadenceDays = settings.CadenceDays,
            ExpectedVersion = settings.ExpectedVersion
        };

        var result = await admittoService.SendAsync(client =>
            client.ConfigureReconfirmPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully configured reconfirm policy.");
        return 0;
    }
}
