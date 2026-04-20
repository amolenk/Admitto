using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--opens-at")]
    [Description("The reconfirmation window open date/time (ISO 8601)")]
    public string? OpensAt { get; set; }

    [CommandOption("--closes-at")]
    [Description("The reconfirmation window close date/time (ISO 8601)")]
    public string? ClosesAt { get; set; }

    [CommandOption("--cadence-days")]
    [Description("Reconfirmation cadence in days (minimum 1)")]
    public int? CadenceDays { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(OpensAt))
        {
            return ValidationResult.Error("--opens-at is required.");
        }

        if (!DateTimeOffset.TryParse(OpensAt, out _))
        {
            return ValidationResult.Error("--opens-at must be a valid ISO 8601 date/time.");
        }

        if (string.IsNullOrWhiteSpace(ClosesAt))
        {
            return ValidationResult.Error("--closes-at is required.");
        }

        if (!DateTimeOffset.TryParse(ClosesAt, out _))
        {
            return ValidationResult.Error("--closes-at must be a valid ISO 8601 date/time.");
        }

        if (CadenceDays is null or < 1)
        {
            return ValidationResult.Error("--cadence-days is required and must be at least 1.");
        }

        return base.Validate();
    }
}

public class SetReconfirmPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SetSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SetSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new SetReconfirmPolicyHttpRequest
        {
            OpensAt = DateTimeOffset.Parse(settings.OpensAt!),
            ClosesAt = DateTimeOffset.Parse(settings.ClosesAt!),
            CadenceDays = settings.CadenceDays!.Value
        };

        var result = await admittoService.SendAsync(client =>
            client.SetReconfirmPolicyAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully set reconfirm policy.");
        return 0;
    }
}