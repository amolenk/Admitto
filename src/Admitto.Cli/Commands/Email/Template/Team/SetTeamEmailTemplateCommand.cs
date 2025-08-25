using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class SetTeamEmailTemplateSettings : TeamSettings
{
    [CommandOption("--emailType")]
    [EmailTypeDescription]
    public EmailType? EmailType { get; init; }

    [CommandOption("--subject")]
    public required string Subject { get; init; }
    
    [CommandOption("--bodyPath")]
    public required string BodyPath { get; init; }
    
    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        if (string.IsNullOrWhiteSpace(Subject))
        {
            return ValidationErrors.EmailSubjectMissing;
        }

        if (string.IsNullOrWhiteSpace(BodyPath))
        {
            return ValidationErrors.EmailBodyPathMissing;
        }
        
        if (!File.Exists(BodyPath))
        {
            return ValidationErrors.EmailBodyPathDoesNotExist;
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

        var request = new SetTeamEmailTemplateRequest
        {
            Subject = settings.Subject,
            Body = await File.ReadAllTextAsync(settings.BodyPath)
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].EmailTemplates[settings.EmailType.ToString()] .PutAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine($"[green]✓ Successfully set team-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}