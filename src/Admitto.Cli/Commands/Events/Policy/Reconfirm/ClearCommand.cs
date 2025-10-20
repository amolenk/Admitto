namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class ClearCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<TeamEventSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Policies.Reconfirm.DeleteAsync());
        if (response is null) return 1;

        outputService.WriteSuccesMessage("Successfully cleared reconfirm policy.");
        return 0;
    }
}