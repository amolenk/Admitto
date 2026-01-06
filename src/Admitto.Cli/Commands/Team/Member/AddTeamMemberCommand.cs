using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Team.Member;

public class AddTeamMemberSettings : TeamSettings
{
    [CommandOption("--email")]
    [Description("The email address")]
    public string? Email { get; init; }

    [CommandOption("--firstName")]
    [Description("The first name")]
    public string? FirstName { get; init; }

    [CommandOption("--lastName")]
    [Description("The last name")]
    public string? LastName { get; init; }

    [CommandOption("--role")]
    [TeamMemberRoleDescription]
    public TeamMemberRole? Role { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationResult.Error("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            return ValidationResult.Error("First name is required.");
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            return ValidationResult.Error("Last name is required.");
        }

        if (Role is null)
        {
            return ValidationResult.Error("Role is required.");
        }

        return base.Validate();
    }
}

public class AddTeamMemberCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<AddTeamMemberSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddTeamMemberSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        
        var request = new AddTeamMemberRequest
        {
            Email = settings.Email,
            FirstName =  settings.FirstName,
            LastName = settings.LastName,
            Role = settings.Role
        };

        var succes = await apiService.CallApiAsync(async client => await client.Teams[teamSlug].Members.PostAsync(request));
        if (!succes) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage($"Successfully added team member '{request.Email}'.");
        return 0;
    }
}