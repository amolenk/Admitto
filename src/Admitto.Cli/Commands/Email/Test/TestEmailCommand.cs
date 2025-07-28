using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Test;

public class TestEmailSettings : TeamEventSettings
{
    [CommandOption("--type")]
    public required EmailType EmailType { get; init; }

    [CommandOption("--recipient")]
    public required string Recipient { get; init; }
    
    [CommandOption("--additionalDetail")]
    public string[]? AdditionalDetails { get; set; } = null!;

    [CommandOption("--ticket")]
    public string[]? Tickets { get; set; }
}

public class TestEmailCommand(
    IAccessTokenProvider accessTokenProvider,
    IConfiguration configuration)
    : ApiCommand<TestEmailSettings>(accessTokenProvider, configuration)
{
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TestEmailSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var request = new TestEmailRequest
        {
            Recipient = settings.Recipient,
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

        var success = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails[settings.EmailType.ToString()].Test.PostAsync(request));

        // TODO we get back a stream, not ideal.
//        if (!success) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully requested '{settings.EmailType}' test mail for '{settings.Recipient}'.[/]");
        return 0;
    }
}
