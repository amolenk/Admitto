using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events;

public class CreateEventSettings : EventSettings
{
    [CommandOption("-s|--slug")]
    public string? EventSlug { get; set; }

    [CommandOption("-n|--name")]
    public string Name { get; set; } = null!;

    [CommandOption("--website")]
    public string Website { get; set; } = null!;

    [CommandOption("--start")]
    public DateTimeOffset? StartTime { get; set; }

    [CommandOption("--end")]
    public DateTimeOffset? EndTime { get; set; }
    
    [CommandOption("--registrationStart")]
    public DateTimeOffset? RegistrationStartTime { get; set; }

    [CommandOption("--registrationEnd")]
    public DateTimeOffset? RegistrationEndTime { get; set; }

    [CommandOption("--baseUrl")]
    public string BaseUrl { get; set; } = null!;
}

public class CreateEventCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<CreateEventSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var request = new CreateTicketedEventRequest
        {
            Slug = eventSlug,
            Name = settings.Name,
            Website = settings.Website,
            StartTime = settings.StartTime,
            EndTime = settings.EndTime,
            RegistrationStartTime = settings.RegistrationStartTime,
            RegistrationEndTime = settings.RegistrationEndTime,
            BaseUrl = settings.BaseUrl,
        };
        
        var succes = await CallApiAsync(async client => await client.Teams[teamSlug].Events.PostAsync(request));
        if (!succes) return 1;
        
        AnsiConsole.MarkupLine($"[green]✓ Successfully created event {settings.Name}.[/]");
        return 0;
    }
}