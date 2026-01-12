using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Reconfirm;

public class ClearReconfirmPolicyCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TeamEventSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var result = await admittoService.SendAsync(client =>
            client.ClearReconfirmPolicyAsync(teamSlug, eventSlug, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully cleared reconfirm policy.");
        return 0;
    }
}