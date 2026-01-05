using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class ClearReconfirmPolicyCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Policies.Reconfirm.DeleteAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully cleared reconfirm policy.");
        return 0;
    }
}