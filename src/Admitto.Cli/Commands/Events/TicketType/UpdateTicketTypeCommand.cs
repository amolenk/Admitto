using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Events.TicketType;

public class UpdateTicketTypeSettings : TeamEventSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the ticket type")]
    public string? Slug { get; set; }
    
    [CommandOption("--maxCapacity")]
    [Description("Maximum available tickets of this type")]
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

public class UpdateTicketTypeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<UpdateTicketTypeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateTicketTypeSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var request = new UpdateTicketTypeRequest()
        {
            MaxCapacity = settings.MaxCapacity
        };
        
        var result = await apiService.CallApiAsync(
            async client => await client.Teams[teamSlug].Events[eventSlug].TicketTypes[settings.Slug].PatchAsync(request));
        if (result is null) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated ticket type.");
        return 0;
    }
}