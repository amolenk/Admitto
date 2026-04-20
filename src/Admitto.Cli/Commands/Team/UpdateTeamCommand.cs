using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team;

public class UpdateTeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    [Description("The slug of the team to update")]
    public string? TeamSlug { get; set; }

    [CommandOption("-n|--name")]
    [Description("The team name")]
    public string? Name { get; init; }

    [CommandOption("--email")]
    [Description("The email address where the team can be reached")]
    public string? Email { get; init; }

    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the team (optimistic concurrency token)")]
    public int? ExpectedVersion { get; init; }
}

public class UpdateTeamCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<UpdateTeamSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpdateTeamSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var request = new UpdateTeamHttpRequest()
        {
            Name = settings.Name,
            EmailAddress = settings.Email,
            ExpectedVersion = settings.ExpectedVersion
        };

        var result =
            await admittoService.SendAsync(client => client.UpdateTeamAsync(teamSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated team.");
        return 0;
    }
}