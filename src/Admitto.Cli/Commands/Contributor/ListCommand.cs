using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Contributor;

public class ListCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<TeamEventSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client => 
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
                $"[grey]{contributor.ContributorId}[/]",
                contributor.Email!,
                $"{contributor.FirstName} {contributor.LastName}",
                string.Join(", ", contributor.Roles!
                    .OrderBy(r => r.Name)
                    .Select(r => r.Name!.FormatContributorRole())),
                contributor.LastChangedAt!.Value.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}