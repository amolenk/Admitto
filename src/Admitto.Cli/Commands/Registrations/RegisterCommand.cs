using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Registrations;

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
}

public class RegisterCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<RegisterSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, RegisterSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new RegisterRequest
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
            IsInvited = true
        };

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Registrations.PostAsync(request));
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully created registration '{response.RegistrationId}'.[/]");
        return 0;
    }
}