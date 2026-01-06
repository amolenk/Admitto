using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class SetEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [Description("The type of email template")]
    public string? EmailType { get; init; }

    [CommandOption("--path")]
    [Description("The path to the folder containing the email template files (subject.txt and body.html)")]
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

public class SetEventEmailTemplateCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<SetEventEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetEventEmailTemplateSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var template = EmailTemplate.Load(settings.TemplateFolderPath);
        
        var request = new SetEventEmailTemplateRequest
        {
            Subject = template.SubjectTemplate,
            TextBody = template.TextBodyTemplate,
            HtmlBody = template.HtmlBodyTemplate
        };
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].EmailTemplates[settings.EmailType]
                .PutAsync(request));
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully set event-level template for '{settings.EmailType}' emails.");
        return 0;
    }
}