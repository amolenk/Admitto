using Amolenk.Admitto.Cli.Commands.Events;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Contributor;

public class AddSettings : TeamEventSettings
{
    [CommandOption("--email")]
    public string? Email { get; set; }

    [CommandOption("--firstName")]
    public string? FirstName { get; set; } = null!;

    [CommandOption("--lastName")]
    public string? LastName { get; set; } = null!;

    [CommandOption("--additionalDetail")]
    public string[]? AdditionalDetails { get; set; } = null!;

    [CommandOption("--role")]
    public string[]? Roles { get; set; } = null!;

    public override ValidationResult Validate()
    {
        if (Email is null)
        {
            return ValidationErrors.EmailMissing;
        }

        if (FirstName is null)
        {
            return ValidationErrors.FirstNameMissing;
        }

        if (LastName is null)
        {
            return ValidationErrors.LastNameMissing;
        }

        if (Roles is null || Roles.Length == 0)
        {
            return ValidationErrors.RoleMissing;
        }
        
        return base.Validate();
    }
}

public class AddCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<AddSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new AddContributorRequest
        {
            Email = settings.Email,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            AdditionalDetails = Parse<AdditionalDetailDto>(
                settings.AdditionalDetails,
                (name, value) => new AdditionalDetailDto
                {
                    Name = name,
                    Value = value
                }),
            Roles = settings.Roles!.Select(r => new ContributorRoleDto { Name = r }).ToList()
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Contributors.PostAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully added contributor (registration ID = '{response.RegistrationId}').[/]");
        return 0;
    }
}