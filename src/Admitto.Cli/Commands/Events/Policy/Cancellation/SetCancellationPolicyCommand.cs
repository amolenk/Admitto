using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Cancellation;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--late-cutoff")]
    [Description("The date/time after which cancellations are considered late (ISO 8601)")]
    public string? LateCancellationCutoff { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(LateCancellationCutoff))
        {
            return ValidationResult.Error("--late-cutoff is required.");
        }

        if (!DateTimeOffset.TryParse(LateCancellationCutoff, out _))
        {
            return ValidationResult.Error("--late-cutoff must be a valid ISO 8601 date/time.");
        }

        return base.Validate();
    }
}

public class SetCancellationPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SetSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var cutoff = DateTimeOffset.Parse(settings.LateCancellationCutoff!);

        var request = new SetCancellationPolicyHttpRequest
        {
            LateCancellationCutoff = cutoff
        };

        var result = await admittoService.SendAsync(client =>
            client.SetCancellationPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully set cancellation policy.");
        return 0;
    }
}
