using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class ClearEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [Description("The type of email template to clear")]
    public string? EmailType { get; init; }

    public override ValidationResult Validate()
    {
        return EmailType is null ? ValidationErrors.EmailTypeMissing : base.Validate();
    }
}

public class ClearEventEmailTemplateCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ClearEventEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ClearEventEmailTemplateSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var result = await admittoService.SendAsync(client =>
            client.ClearEventEmailTemplateAsync(teamSlug, eventSlug, settings.EmailType, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully cleared event-level template for '{settings.EmailType}' emails.");
        return 0;
    }
}