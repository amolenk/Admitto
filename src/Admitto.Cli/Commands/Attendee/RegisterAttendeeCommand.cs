using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class RegisterAttendeeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address of the attendee")]
    public string? Email { get; init; }

    [CommandOption("--firstName")]
    [Description("The first name of the attendee")]
    public string? FirstName { get; init; }

    [CommandOption("--lastName")]
    [Description("The last name of the attendee")]
    public string? LastName { get; init; }

    [CommandOption("--detail")]
    [Description("Additional attendee information in the format 'Name=Value'")]
    public string[]? AdditionalDetails { get; init; }

    [CommandOption("--ticket")]
    [Description("Ticket(s) to register")]
    public string[]? Tickets { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationErrors.EmailMissing;
        }

        if (string.IsNullOrWhiteSpace(FirstName))
        {
            return ValidationErrors.FirstNameMissing;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            return ValidationErrors.LastNameMissing;
        }

        if (Tickets is null || Tickets.Length == 0)
        {
            return ValidationErrors.TicketsMissing;
        }

        return base.Validate();
    }
}

public class RegisterAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<RegisterAttendeeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RegisterAttendeeSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new RegisterAttendeeRequest
        {
            Email = settings.Email,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            AdditionalDetails = InputHelper.ParseAdditionalDetails(settings.AdditionalDetails),
            AssignedTickets = InputHelper.ParseTickets(settings.Tickets)
        };

        var succes =
            await apiService.CallApiAsync(async client =>
                await client.Teams[teamSlug].Events[eventSlug].Attendees.PostAsync(request));
        if (!succes) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully registered attendee.");
        return 0;
    }
}