using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class DenyAttendeeVisaLetterSettings : TeamEventSettings
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

public class DenyAttendeeVisaLetterCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<DenyAttendeeVisaLetterSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DenyAttendeeVisaLetterSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeeId = await apiService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
        if (attendeeId is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"Attendee with email '{settings.Email}' not found.");
            return 1;
        }
        
        if (!AnsiConsoleExt.Confirm("Deny visa letter (registration will be canceled)?"))
        {
            return 0;
        }
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[attendeeId.Value].DenyVisa.PostAsync());
        if (response is null) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage("Successfully cancelled registration.");
        return 0;
    }
}