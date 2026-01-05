using Amolenk.Admitto.Cli.Common;

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

public class ReconfirmAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ReconfirmSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ReconfirmSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeeId = await apiService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
        if (attendeeId is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"Attendee with email '{settings.Email}' not found.");
            return 1;
        }

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[attendeeId.Value].Reconfirm.PostAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully reconfirmed registration.");
        return 0;
    }
}