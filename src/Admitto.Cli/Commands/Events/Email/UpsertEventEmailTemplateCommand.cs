using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Email;

public class UpsertEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [Description("The type of email template (e.g. 'ticket')")]
    public string? EmailType { get; init; }

    [CommandOption("--path")]
    [Description("Path to the folder containing the template files (subject.txt, body.txt, body.html)")]
    public required string TemplateFolderPath { get; init; }

    [CommandOption("--expected-version <version>")]
    [Description("The expected current version of the template (optimistic concurrency token). Omit when creating.")]
    public int? ExpectedVersion { get; init; }

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

public class UpsertEventEmailTemplateCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<UpsertEventEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpsertEventEmailTemplateSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new UpsertEmailTemplateHttpRequest
        {
            Subject = File.ReadAllText(Path.Combine(settings.TemplateFolderPath, "subject.txt")),
            TextBody = File.ReadAllText(Path.Combine(settings.TemplateFolderPath, "body.txt")),
            HtmlBody = File.ReadAllText(Path.Combine(settings.TemplateFolderPath, "body.html")),
            Version = settings.ExpectedVersion
        };

        var success = await admittoService.SendAsync(
            client => client.UpsertEventEmailTemplateAsync(teamSlug, eventSlug, settings.EmailType!, request, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated event-level '{settings.EmailType}' email template for '{eventSlug}'.");
        return 0;
    }
}
