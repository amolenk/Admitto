using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class UpdateAttendeeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address of the attendee")]
    public string? Email { get; init; }
    
    [CommandOption("--ticket")]
    [Description("Ticket(s) to include for the attendee (will replace existing tickets)")]
    public string[]? Tickets { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationErrors.EmailMissing;
        }

        if (Tickets is null || Tickets.Length == 0)
        {
            return ValidationErrors.TicketsMissing;
        }

        return base.Validate();
    }
}

public class UpdateAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<UpdateAttendeeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateAttendeeSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeeId = await apiService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
        if (attendeeId is null)
        {
            AnsiConsoleExt.WriteErrorMessage($"Attendee with email '{settings.Email}' not found.");
            return 1;
        }

        var request = new UpdateAttendeeRequest
        {
            Tickets = InputHelper.ParseTickets(settings.Tickets)
        };

        var response =
            await apiService.CallApiAsync(async client =>
                await client.Teams[teamSlug].Events[eventSlug].Attendees[attendeeId.Value].PutAsync(request));
        if (response is null) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated attendee.");
        return 0;
    }
}