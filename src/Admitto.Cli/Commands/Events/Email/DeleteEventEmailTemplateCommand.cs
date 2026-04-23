using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Email;

public class DeleteEventEmailTemplateSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [Description("The type of email template (e.g. 'ticket')")]
    public string? EmailType { get; init; }

    [CommandOption("--version <version>")]
    [Description("The expected current version of the template (optimistic concurrency token)")]
    public int? Version { get; init; }

    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        if (!Version.HasValue)
        {
            return ValidationResult.Error("Missing required option --version.");
        }

        return base.Validate();
    }
}

public class DeleteEventEmailTemplateCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<DeleteEventEmailTemplateSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        DeleteEventEmailTemplateSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var success = await admittoService.SendAsync(
            client => client.DeleteEventEmailTemplateAsync(teamSlug, eventSlug, settings.EmailType!, settings.Version!.Value, cancellationToken));

        if (!success) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully deleted event-level '{settings.EmailType}' email template for '{eventSlug}'.");
        return 0;
    }
}
