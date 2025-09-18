namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class SetEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--type")]
    public string? EmailType { get; init; }

    [CommandOption("--path")]
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

public class SetEventEmailTemplateCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<SetEventEmailTemplateSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetEventEmailTemplateSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var template = EmailTemplate.Load(settings.TemplateFolderPath);
        
        var request = new SetEventEmailTemplateRequest
        {
            Subject = template.SubjectTemplate,
            Body = template.BodyTemplate
        };
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].EmailTemplates[settings.EmailType]
                .PutAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine($"[green]✓ Successfully set event-level template for '{settings.EmailType}' emails.[/]");
        return 0;
    }
}