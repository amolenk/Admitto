using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

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

public class UpdateAttendeeCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<UpdateAttendeeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateAttendeeSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var attendeeId = await admittoService.FindAttendeeAsync(teamSlug, eventSlug, settings.Email!);
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
            await admittoService.QueryAsync(client => client.GetAttendeeAsync(
                teamSlug,
                eventSlug,
                attendeeId.Value,
                cancellationToken));
        if (response is null) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated attendee.");
        return 0;
    }
}