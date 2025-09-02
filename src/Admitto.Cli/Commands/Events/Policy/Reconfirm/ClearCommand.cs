using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class ClearCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<TeamEventSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Policies.Reconfirm.DeleteAsync());
        if (response is null) return 1;

        AnsiConsole.MarkupLine(
            $"[green]✓ Successfully cleared reconfirm policy.[/]");
        return 0;
    }
}