namespace Amolenk.Admitto.Cli.Commands.Events.TicketType;

public class AddTicketTypeSettings : TeamEventSettings
{
    [CommandOption("-s|--slug")]
    public string? Slug { get; set; }
    
    [CommandOption("-n|--name")]
    public string? Name { get; set; } = null!;

    [CommandOption("--slotName")]
    public string[]? SlotName { get; set; }

    [CommandOption("--maxCapacity")]
    public int? MaxCapacity { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Slug))
        {
            return ValidationErrors.TicketTypeSlugMissing;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationErrors.TicketTypeNameMissing;
        }

        return base.Validate();
    }
}

public class AddTicketTypeCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : EventCommandBase<AddTicketTypeSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddTicketTypeSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var request = new AddTicketTypeRequest
        {
            Slug = settings.Slug,
            Name = settings.Name,
            SlotNames = (settings.SlotName ?? []).ToList(),
            MaxCapacity = settings.MaxCapacity
        };
        
        var succes = await CallApiAsync(
            async client => await client.Teams[teamSlug].Events[eventSlug].TicketTypes.PostAsync(request));
        if (!succes) return 1;
        
        outputService.WriteSuccesMessage($"Successfully added ticket type {settings.Name}.");
        return 0;
    }
}