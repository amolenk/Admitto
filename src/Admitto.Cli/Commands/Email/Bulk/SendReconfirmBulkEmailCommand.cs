using Amolenk.Admitto.Cli.Common;

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

public class SendReconfirmBulkEmailCommand(IApiService apiService, IConfigService configService)
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
            InitialDelayAfterRegistration = settings.InitialDelayAfterRegistration!.Value.ToString(),
            ReminderInterval = settings.ReminderInterval is not null
                ? TimeSpan.FromDays(settings.ReminderInterval.Value).ToString()
                : null
        };

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk.Reconfirm
                .PostAsync(request, cancellationToken: cancellationToken));
        if (!response) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully requested reconfirm email bulk.");
        return 0;
    }
}