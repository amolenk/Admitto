using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Member;

public class UpdateTeamMemberSettings : TeamSettings
{
    [CommandOption("--email")]
    [Description("The email address of the team member")]
    public string? Email { get; init; }

    [CommandOption("--role")]
    [TeamMemberRoleDescription]
    public TeamMemberRole? Role { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationResult.Error("Email is required.");
        }

        if (Role is null)
        {
            return ValidationResult.Error("Role is required.");
        }

        return base.Validate();
    }
}

public class UpdateTeamMemberCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<UpdateTeamMemberSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpdateTeamMemberSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var request = new UpdateTeamMemberRequest
        {
            NewRole = settings.Role!.Value
        };

        var success = await admittoService.SendAsync(
            client => client.UpdateTeamMemberAsync(teamSlug, settings.Email!, request, cancellationToken));
        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated team member '{settings.Email}'.");
        return 0;
    }
}
