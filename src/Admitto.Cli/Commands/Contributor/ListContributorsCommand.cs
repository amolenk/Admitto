using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Contributor;

public class ListContributorsCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client => 
            await client.Teams[teamSlug].Events[eventSlug].Contributors.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("ID");
        table.AddColumn("Email");
        table.AddColumn("Name");
        table.AddColumn("Status");
        table.AddColumn("Last updated");

        foreach (var contributor in response.Contributors!
                     .OrderByDescending(r => r.LastChangedAt!.Value))
        {
            table.AddRow(
                contributor.ContributorId!.Value.ToString(),
                contributor.Email!,
                $"{contributor.FirstName} {contributor.LastName}",
                string.Join(", ", contributor.Roles!
                    .Select(r => r.FormatContributorRole())),
                contributor.LastChangedAt!.Value.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}