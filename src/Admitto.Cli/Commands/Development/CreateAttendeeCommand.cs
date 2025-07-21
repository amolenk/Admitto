using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Development;

public class CreateAttendeeSettings : TeamEventSettings
{
    [CommandOption("--email")]
    public string? Email { get; set; }

    [CommandOption("--firstName")]
    public string? FirstName { get; set; } = null!;

    [CommandOption("--lastName")]
    public string? LastName { get; set; } = null!;

    [CommandOption("--additionalDetail")]
    public string[]? AdditionalDetails { get; set; } = null!;

    [CommandOption("--ticket")]
    public string[]? Tickets { get; set; }
}

public class CreateAttendeeCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<CreateAttendeeSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateAttendeeSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new StartRegistrationRequest
        {
            Email = settings.Email,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            AdditionalDetails = Parse<AdditionalDetail>(
                settings.AdditionalDetails,
                (name, value) => new AdditionalDetail
                {
                    Name = name,
                    Value = value
                }),
            Tickets = Parse<TicketSelection, int>(
                settings.Tickets,
                (ticketTypeSlug, quantity) => new TicketSelection
                {
                    TicketTypeSlug = ticketTypeSlug,
                    Quantity = quantity
                })
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees.PostAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully created attendee '{response.AttendeeId}'.[/]");
        return 0;
    }
}