using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class SetTeamEmailTemplateSettings : TeamSettings
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

public class SetTeamEmailTemplateCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SetTeamEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        SetTeamEmailTemplateSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var template = EmailTemplate.Load(settings.TemplateFolderPath);

        var request = new SetTeamEmailTemplateRequest
        {
            Subject = template.SubjectTemplate,
            HtmlBody = template.HtmlBodyTemplate
        };

        var result = await admittoService.SendAsync(client =>
            client.SetTeamEmailTemplateAsync(teamSlug, settings.EmailType, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully set team-level template for '{settings.EmailType}' emails.");
        return 0;
    }
}