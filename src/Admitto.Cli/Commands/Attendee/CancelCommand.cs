using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class CancelSettings : TeamEventSettings
{
    [CommandOption("--id")]
    public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        if (Id is null)
        {
            return ValidationErrors.IdMissing;
        }

        return base.Validate();
    }
}

public class CancelCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<CancelSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancelSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.Id!.Value].DeleteAsync());
        if (response is null) return 1;
        
        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully cancelled registration.[/]");
        return 0;
    }
}