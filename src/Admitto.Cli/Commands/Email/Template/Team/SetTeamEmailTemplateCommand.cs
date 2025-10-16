namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class SetTeamEmailTemplateSettings : TeamSettings
{
    [CommandOption("--type")]
    public string? EmailType { get; init; }

    [CommandOption("--path")]
    [Description("The path to the folder containing the email template files (subject.txt and body.html).")]
    public required string TemplateFolderPath { get; init; }
    
    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        if (string.IsNullOrWhiteSpace(TemplateFolderPath))
        {
            return ValidationErrors.EmailTemplateFolderPathMissing;
        }
        
        if (!Directory.Exists(TemplateFolderPath))
        {
            return ValidationErrors.EmailTemplateFolderPathDoesNotExist;
        }
        
        return base.Validate();
    }
}

public class SetTeamEmailTemplateCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<SetTeamEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetTeamEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);

        var template = EmailTemplate.Load(settings.TemplateFolderPath);
        
        var request = new SetTeamEmailTemplateRequest
        {
            Subject = template.SubjectTemplate,
            HtmlBody = template.HtmlBodyTemplate
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].EmailTemplates[settings.EmailType] .PutAsync(request));
        if (response is null) return 1;

        outputService.WriteSuccesMessage($"Successfully set team-level template for '{settings.EmailType}' emails.");
        return 0;
    }
}