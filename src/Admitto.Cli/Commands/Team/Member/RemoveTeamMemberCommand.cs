using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Member;

public class RemoveTeamMemberSettings : TeamSettings
{
    [CommandOption("--email")]
    [Description("The email address of the team member to remove")]
    public string? Email { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationResult.Error("Email is required.");
        }

        return base.Validate();
    }
}

public class RemoveTeamMemberCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<RemoveTeamMemberSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        RemoveTeamMemberSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var success = await admittoService.SendAsync(
            client => client.RemoveTeamMemberAsync(teamSlug, settings.Email!, cancellationToken));
        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully removed team member '{settings.Email}'.");
        return 0;
    }
}
