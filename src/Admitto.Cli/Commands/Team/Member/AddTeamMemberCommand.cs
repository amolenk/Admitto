using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team.Member;

public class AddTeamMemberSettings : TeamSettings
{
    [CommandOption("--email")]
    [Description("The email address")]
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

public class AddTeamMemberCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<AddTeamMemberSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AddTeamMemberSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var request = new AssignTeamMemberV2Request
        {
            Email = settings.Email!,
            Role = settings.Role!.Value
        };

        var succes =
            await admittoService.SendAsync(client => client.AssignTeamMemberV2Async(teamSlug, request, cancellationToken));
        if (!succes) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully added team member '{request.Email}'.");
        return 0;
    }
}