using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Cancellation;

public class ConfigureCancellationPolicySettings : TeamEventSettings
{
    [CommandOption("--late-cutoff")]
    [Description("Cut-off date/time after which cancellations are considered late (ISO 8601). Omit to clear.")]
    public string? LateCancellationCutoff { get; set; }

    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the event (optimistic concurrency token).")]
    public int? ExpectedVersion { get; set; }
}

public class ConfigureCancellationPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ConfigureCancellationPolicySettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ConfigureCancellationPolicySettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        DateTimeOffset? lateCutoff = null;
        if (!string.IsNullOrWhiteSpace(settings.LateCancellationCutoff))
        {
            if (!DateTimeOffset.TryParse(settings.LateCancellationCutoff, out var parsed))
            {
                AnsiConsoleExt.WriteErrorMessage("Invalid --late-cutoff value. Expected ISO 8601 date/time.");
                return 1;
            }

            lateCutoff = parsed;
        }

        var request = new ConfigureCancellationPolicyHttpRequest
        {
            LateCancellationCutoff = lateCutoff,
            ExpectedVersion = settings.ExpectedVersion
        };

        var result = await admittoService.SendAsync(client =>
            client.ConfigureCancellationPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully configured cancellation policy.");
        return 0;
    }
}
