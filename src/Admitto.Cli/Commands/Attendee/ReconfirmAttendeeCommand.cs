using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ReconfirmSettings : TeamEventSettings
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

public class ReconfirmAttendeeCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<ReconfirmSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        ReconfirmSettings settings,
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

        var succes =
            await admittoService.SendAsync(client =>
                client.ReconfirmRegistrationAsync(teamSlug, eventSlug, attendeeId.Value, cancellationToken));
        if (!succes) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully reconfirmed registration.");
        return 0;
    }
}