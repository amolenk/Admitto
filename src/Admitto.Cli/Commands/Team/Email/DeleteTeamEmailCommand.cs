using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Email;

public class DeleteTeamEmailSettings : TeamSettings
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

public class DeleteTeamEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<DeleteTeamEmailSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeleteTeamEmailSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var success = await admittoService.SendAsync(
            client => client.DeleteTeamEmailSettingsAsync(teamSlug, null, settings.Version!.Value, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully deleted email settings for team '{teamSlug}'.");
        return 0;
    }
}
