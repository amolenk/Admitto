using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

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

public class SendReconfirmEmailCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<SendReconfirmEmailSettings>
{
    public sealed override async Task<int> ExecuteAsync(
        CommandContext context,
        SendReconfirmEmailSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeeId = await admittoService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
        if (attendeeId is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"Attendee with email '{settings.Email}' not found.");
            return 1;
        }

        var request = new SendReconfirmEmailRequest
        {
            AttendeeId = attendeeId.Value
        };

        if (!AnsiConsoleExt.Confirm(
                $"Are you sure you want to send a reconfirmation email to {settings.Email}?"))
        {
            AnsiConsoleExt.WriteSuccesMessage("Aborted.");
            return 0;
        }


        var result = await admittoService.SendAsync(client =>
            client.SendReconfirmEmailAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully sent reconfirmation mail.");
        return 0;
    }
}