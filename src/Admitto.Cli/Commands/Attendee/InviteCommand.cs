using Amolenk.Admitto.Cli.Commands.Events;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class InviteSettings : TeamEventSettings
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
    
    public override ValidationResult Validate()
    {
        if (Email is null)
        {
            return ValidationErrors.EmailMissing;
        }

        if (FirstName is null)
        {
            return ValidationErrors.FirstNameMissing;
        }

        if (LastName is null)
        {
            return ValidationErrors.LastNameMissing;
        }

        if (Tickets is null)
        {
            return ValidationErrors.TicketsMissing;
        }

        return base.Validate();
    }
}

public class InviteCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<InviteSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, InviteSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new InviteAttendeeRequest
        {
            Email = settings.Email,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            AdditionalDetails = Parse<AdditionalDetailDto>(
                settings.AdditionalDetails,
                (name, value) => new AdditionalDetailDto
                {
                    Name = name,
                    Value = value
                }),
            Tickets = Parse<TicketSelectionDto, int>(
                settings.Tickets,
                (ticketTypeSlug, quantity) => new TicketSelectionDto
                {
                    TicketTypeSlug = ticketTypeSlug,
                    Quantity = quantity
                })
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].AttendeeRegistrations.Invite.PostAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully registered attendee (ID = '{response.RegistrationId}').[/]");
        return 0;
    }
}