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
}

public class RegisterAttendeeCommand(
    OutputService outputService,
    IApiService apiService,
    IConfiguration configuration)
    : AsyncCommand<RegisterAttendeeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RegisterAttendeeSettings settings)
    {
        var teamSlug = settings.TeamSlug ?? configuration[ConfigSettings.DefaultTeamSetting];
        var eventSlug = settings.EventSlug ?? configuration[ConfigSettings.DefaultEventSetting];
        var request = new RegisterAttendeeRequest
        {
            Email = settings.Email,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            AdditionalDetails = ParseAdditionalDetails(settings.AdditionalDetails),
            AssignedTickets = ParseTickets(settings.Tickets)
        };

        var succes =
            await apiService.CallApiAsync(async client =>
                await client.Teams[teamSlug].Events[eventSlug].Attendees.PostAsync(request));
        if (!succes) return 1;

        outputService.WriteSuccesMessage($"Successfully registered attendee.");
        return 0;
    }

    private static List<AdditionalDetailDto> ParseAdditionalDetails(string[]? additionalDetails)
    {
        var result = new List<AdditionalDetailDto>();

        foreach (var additionalDetail in additionalDetails ?? [])
        {
            var parts = additionalDetail.Split('=', 2);
            if (parts.Length != 2)
            {
                throw new ArgumentException(
                    $"Invalid additional detail format: '{additionalDetail}'. Expected format is 'Name=Value'.");
            }
            
            result.Add(
                new AdditionalDetailDto
                {
                    Name = parts[0],
                    Value = parts[1]
                });
        }

        return result;
    }

    private static List<TicketSelectionDto> ParseTickets(string[]? tickets)
    {
        return (tickets ?? [])
            .Select(t => new TicketSelectionDto
            {
                TicketTypeSlug = t,
                Quantity = 1
            })
            .ToList();
    }
}