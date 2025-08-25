using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events.TicketType;

public class AddTicketTypeSettings : EventSettings
{
    [CommandOption("-e|--event")]
    public string? EventSlug { get; set; }
    
    [CommandOption("-s|--slug")]
    public string? Slug { get; set; }
    
    [CommandOption("-n|--name")]
    public string? Name { get; set; } = null!;

    [CommandOption("--slotName")]
    public string[]? SlotName { get; set; }

    [CommandOption("--maxCapacity")]
    public int? MaxCapacity { get; set; }
}

public class AddTicketTypeCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : EventCommandBase<AddTicketTypeSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddTicketTypeSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var request = new AddTicketTypeRequest
        {
            Slug = settings.Slug,
            Name = settings.Name,
            SlotName = string.Join(";", settings.SlotName!),
            MaxCapacity = settings.MaxCapacity
        };
        
        var succes = await CallApiAsync(
            async client => await client.Teams[teamSlug].Events[eventSlug].TicketTypes.PostAsync(request));
        if (!succes) return 1;
        
        AnsiConsole.MarkupLine($"[green]✓ Successfully added ticket type {settings.Name}.[/]");
        return 0;
    }
}