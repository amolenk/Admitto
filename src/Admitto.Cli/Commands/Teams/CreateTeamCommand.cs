using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Teams;

public class CreateTeamSettings : CommandSettings
{
    [CommandOption("-s|--slug")]
    public string? TeamSlug { get; set; }
    
    [CommandOption("-n|--name")]
    public string? Name { get; set; } = null!;

    [CommandOption("--senderEmail")]
    public string? SenderEmail { get; set; } = null!;

    [CommandOption("--smtpServer")]
    public string? SmtpServer { get; set; } = null!;

    [CommandOption("--smtpPort")]
    [DefaultValue(587)]
    public int SmtpPort { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(SenderEmail))
        {
            return ValidationResult.Error("Sender e-mail is required.");
        }

        if (string.IsNullOrWhiteSpace(SmtpServer))
        {
            return ValidationResult.Error("SMTP server is required.");
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
            EmailSettings = new EmailSettingsDto()
            {
                SenderEmail = settings.SenderEmail,
                SmtpServer = settings.SmtpServer,
                SmtpPort = settings.SmtpPort
            }
        };

        var succes = await CallApiAsync(async client => await client.Teams.PostAsync(request));
        if (!succes) return 1;
        
        AnsiConsole.MarkupLine($"[green]✓ Successfully created team {request.Name}.[/]");
        return 0;
    }
}