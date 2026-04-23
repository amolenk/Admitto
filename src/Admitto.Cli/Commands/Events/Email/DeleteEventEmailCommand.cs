using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Email;

public class DeleteEventEmailSettings : TeamEventSettings
{
    [CommandOption("--version <version>")]
    [Description("The expected current version of the email settings (optimistic concurrency token)")]
    public int? Version { get; init; }

    public override ValidationResult Validate()
    {
        if (!Version.HasValue)
        {
            return ValidationResult.Error("Missing required option --version.");
        }

        return base.Validate();
    }
}

public class DeleteEventEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<DeleteEventEmailSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeleteEventEmailSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var success = await admittoService.SendAsync(
            client => client.DeleteEventEmailSettingsAsync(teamSlug, eventSlug, settings.Version!.Value, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully deleted email settings for event '{eventSlug}'.");
        return 0;
    }
}
