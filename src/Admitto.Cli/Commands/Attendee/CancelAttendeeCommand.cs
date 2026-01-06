using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class CancelSettings : TeamEventSettings
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

public class CancelAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<CancelSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancelSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeeId = await apiService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
        if (attendeeId is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"Attendee with email '{settings.Email}' not found.");
            return 1;
        }
        
        if (!AnsiConsoleExt.Confirm("Cancel registration?"))
        {
            return 0;
        }

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[attendeeId.Value].DeleteAsync());
        if (response is null) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage("Successfully cancelled registration.");
        return 0;
    }
}