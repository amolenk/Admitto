using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team;

public class ArchiveTeamSettings : CommandSettings
{
    [CommandArgument(0, "<team-slug>")]
    [Description("The slug of the team to archive")]
    public string TeamSlug { get; init; } = string.Empty;

    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the team (optimistic concurrency token)")]
    public uint? ExpectedVersion { get; init; }

    public override ValidationResult Validate()
    {
        if (ExpectedVersion is null)
        {
            return ValidationResult.Error("--expected-version is required.");
        }

        return base.Validate();
    }
}

public class ArchiveTeamCommand(IAdmittoService admittoService)
    : AsyncCommand<ArchiveTeamSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ArchiveTeamSettings settings,
        CancellationToken cancellationToken)
    {
        var request = new ArchiveTeamRequest
        {
            ExpectedVersion = settings.ExpectedVersion!.Value
        };

        var success = await admittoService.SendAsync(
            client => client.ArchiveTeamAsync(settings.TeamSlug, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully archived team '{settings.TeamSlug}'.");
        return 0;
    }
}
