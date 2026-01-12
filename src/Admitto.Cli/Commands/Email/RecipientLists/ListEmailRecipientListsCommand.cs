using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Email.RecipientLists;

public class ListEmailRecipientListsCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await admittoService.QueryAsync(client =>
            client.GetEmailRecipientListsAsync(teamSlug, eventSlug, cancellationToken)); 
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Name");

        foreach (var name in response.Names!.OrderBy(n => n))
        {
            table.AddRow(name);
        }

        AnsiConsole.Write(table);
        return 0;
    }
}