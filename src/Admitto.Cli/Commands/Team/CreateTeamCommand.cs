using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Teams;

public class CreateTeamSettings : CommandSettings
{
    [CommandOption("-s|--slug")]
    public required string TeamSlug { get; set; }
    
    [CommandOption("-n|--name")]
    public required string Name { get; set; } = null!;

    [CommandOption("--email")]
    public required string Email { get; set; } = null!;

    [CommandOption("--emailServiceConnectionString")]
    public required string EmailServiceConnectionString { get; set; } = null!;

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationResult.Error("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(EmailServiceConnectionString))
        {
            return ValidationResult.Error("Email service connection string is required.");
        }

        return base.Validate();
    }
}

public class CreateTeamCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<CreateTeamSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateTeamSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var request = new CreateTeamRequest()
        {
            Slug = teamSlug,
            Name = settings.Name,
            Email = settings.Email,
            EmailServiceConnectionString = settings.EmailServiceConnectionString
        };

        var succes = await CallApiAsync(async client => await client.Teams.PostAsync(request));
        if (!succes) return 1;
        
        AnsiConsole.MarkupLine($"[green]✓ Successfully created team {request.Name}.[/]");
        return 0;
    }
}