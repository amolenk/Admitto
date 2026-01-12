using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class SendReconfirmBulkEmailSettings : TeamEventSettings
{
    [CommandOption("--initial-delay")]
    [Description("The number of days to wait after registration before sending the first reconfirmation email.")]
    public int? InitialDelayAfterRegistration { get; set; }

    [CommandOption("--reminder-interval")]
    [Description("Days between reconfirmation reminder emails.")]
    public int? ReminderInterval { get; set; }

    public override ValidationResult Validate()
    {
        if (InitialDelayAfterRegistration is null)
        {
            return ValidationErrors.ReconfirmInitialDelayMissing;
        }

        return base.Validate();
    }
}

public class SendReconfirmBulkEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SendReconfirmBulkEmailSettings>
{
    public sealed override async Task<int> ExecuteAsync(
        CommandContext context,
        SendReconfirmBulkEmailSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        AnsiConsoleExt.WriteWarningMessage(
            "You are about to send a reconfirmation request email to one or more recipients.");

        AnsiConsoleExt.WriteWarningMessage(
            "It is recommended to configure a reconfirmation policy for this event to ensure consistent handling of reconfirmations.");

        if (!AnsiConsoleExt.Confirm("Are you sure you want to proceed?"))
        {
            AnsiConsoleExt.WriteSuccesMessage("Aborted.");
            return 0;
        }

        var request = new SendReconfirmBulkEmailRequest
        {
            InitialDelayAfterRegistration = TimeSpan.FromDays(settings.InitialDelayAfterRegistration!.Value),
            ReminderInterval = settings.ReminderInterval is not null
                ? TimeSpan.FromDays(settings.ReminderInterval.Value)
                : null
        };

        var result = await admittoService.SendAsync(client =>
            client.SendReconfirmBulkEmailAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully requested reconfirm email bulk.");
        return 0;
    }
}