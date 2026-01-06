using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.RecipientLists;

public class ListEmailRecipientListsCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client => 
            await client.Teams[teamSlug].Events[eventSlug].EmailRecipientLists.GetAsync());
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