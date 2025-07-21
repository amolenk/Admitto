using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class SetTeamEmailTemplateSettings : TeamSettings
{
    [CommandOption("--emailType")]
    public EmailType? EmailType { get; set; }

    [CommandOption("--subject")]
    public string Subject { get; set; } = "Default Subject";

    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationResult.Error("Email type is required.");
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            return ValidationResult.Error("Subject cannot be empty.");
        }
        
        return base.Validate();
    }
}

public class SetTeamEmailTemplateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<SetTeamEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetTeamEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);

        var request = new ConfigureTeamEmailTemplateRequest
        {
            Subject = "Subject",
            Body = "Body"
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Email.Templates[settings.EmailType.ToString()] .PutAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine($"[green]✓ Successfully set team-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}