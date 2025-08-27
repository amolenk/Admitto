using Amolenk.Admitto.Cli.Commands.Events;
using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class RegisterSettings : TeamEventSettings
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
    
    [CommandOption("--verificationToken")]
    public string? VerificationToken { get; set; } = null!;
    
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

        if (VerificationToken is null)
        {
            return ValidationErrors.VerificationTokenMissing;
        }

        return base.Validate();
    }
}

public class RegisterCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<RegisterSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, RegisterSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new RegisterAttendeeRequest
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
                }),
            VerificationToken = settings.VerificationToken
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].AttendeeRegistrations.PostAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully registered attendee (ID = '{response.RegistrationId}').[/]");
        return 0;
    }
}