namespace Amolenk.Admitto.Cli.Commands.Events.TicketType;

public class UpdateTicketTypeSettings : TeamEventSettings
{
    [CommandOption("-s|--slug")]
    public string? Slug { get; set; }
    
    [CommandOption("--maxCapacity")]
    public int? MaxCapacity { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Slug))
        {
            return ValidationErrors.TicketTypeSlugMissing;
        }

        if (MaxCapacity is null)
        {
            return ValidationErrors.TicketTypeMaxCapacityMissing;
        }

        return base.Validate();
    }
}

public class UpdateTicketTypeCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : EventCommandBase<UpdateTicketTypeSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateTicketTypeSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var request = new UpdateTicketTypeRequest()
        {
            MaxCapacity = settings.MaxCapacity
        };
        
        var result = await CallApiAsync(
            async client => await client.Teams[teamSlug].Events[eventSlug].TicketTypes[settings.Slug].PatchAsync(request));
        if (result is null) return 1;
        
        outputService.WriteSuccesMessage($"Successfully updated ticket type.");
        return 0;
    }
}