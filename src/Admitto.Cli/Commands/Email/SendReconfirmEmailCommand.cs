using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email;

public class SendReconfirmEmailSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address of the attendee")]
    public string? Email { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationErrors.EmailMissing;
        }

        return base.Validate();
    }
}

public class SendReconfirmEmailCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<SendReconfirmEmailSettings>
{
    public sealed override async Task<int> ExecuteAsync(
        CommandContext context,
        SendReconfirmEmailSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeeId = await apiService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
        if (attendeeId is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"Attendee with email '{settings.Email}' not found.");
            return 1;
        }

        var request = new SendReconfirmEmailRequest
        {
            AttendeeId = attendeeId
        };

        if (!AnsiConsoleExt.Confirm(
                $"Are you sure you want to send a reconfirmation email to {settings.Email}?"))
        {
            AnsiConsoleExt.WriteSuccesMessage("Aborted.");
            return 0;
        }


        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails.Reconfirm.PostAsync(request));
        if (!response) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully sent reconfirmation mail.");
        return 0;
    }
}