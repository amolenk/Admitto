using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class SendCustomBulkEmailSettings : TeamEventSettings
{
    [CommandOption("--type")]
    [Description("The type of bulk email to schedule")]
    public string? EmailType { get; init; }

    [CommandOption("--list")]
    [Description("The email recipient list to send to")]
    public string? ListName { get; init; }

    [CommandOption("--excludeAttendees")]
    [Description("Whether or not to exclude attendees from the email bulk")]
    public bool? ExcludeAttendees { get; init; }

    [CommandOption("--key")]
    [Description(
        "The idempotency key of the bulk email. Bulk emails with the same key are deduplicated per recipient.")]
    public string? IdempotencyKey { get; init; }

    public override ValidationResult Validate()
    {
        if (EmailType is null)
        {
            return ValidationErrors.EmailTypeMissing;
        }

        if (ListName is null)
        {
            return ValidationErrors.EmailRecipientListMissing;
        }

        return base.Validate();
    }
}

public class SendCustomBulkEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SendCustomBulkEmailSettings>
{
    public sealed override async Task<int> ExecuteAsync(
        CommandContext context,
        SendCustomBulkEmailSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        AnsiConsoleExt.WriteWarningMessage("You are about to send a bulk email to multiple recipients.");

        if (!settings.ExcludeAttendees.HasValue)
        {
            AnsiConsoleExt.WriteWarningMessage(
                "If the list contains attendees, they are included in the email bulk by default."
                + " If you want to exclude them, please re-run the command with the '--excludeAttendees' flag.");
        }

        var verifyListName = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the recipient list name to reconfirm:"));

        if (verifyListName != settings.ListName)
        {
            AnsiConsoleExt.WriteErrorMessage("Recipient list name does not match. Aborting.");
            return 1;
        }

        // By default, base the idempotency key on the combination of email type and list name.
        // This ensures that the recipients on the list will receive the email of the specified type at most once.
        var idempotencyKey = settings.IdempotencyKey ?? $"{settings.EmailType}/{settings.ListName}";

        var request = new SendCustomBulkEmailRequest
        {
            EmailType = settings.EmailType,
            RecipientListName = settings.ListName,
            ExcludeAttendees = settings.ExcludeAttendees!.Value,
            IdempotencyKey = idempotencyKey
        };

        var result = await admittoService.SendAsync(client =>
            client.SendCustomBulkEmailAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully requested {settings.EmailType} email bulk for {settings.ListName} recipient list.");
        return 0;
    }
}