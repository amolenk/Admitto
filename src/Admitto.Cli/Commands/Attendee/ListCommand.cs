using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ListCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<TeamEventSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client => 
            await client.Teams[teamSlug].Events[eventSlug].Attendees.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Email");
        table.AddColumn("Name");
        table.AddColumn("Status");
        table.AddColumn("Last updated");

        foreach (var attendee in response.Attendees!
                     .OrderByDescending(r => r.LastChangedAt!.Value))
        {
            table.AddRow(
                $"[grey]{attendee.AttendeeId}[/]",
                attendee.Email!,
                $"{attendee.FirstName} {attendee.LastName}",
                attendee.Status!.Value.Format(),
                attendee.LastChangedAt!.Value.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}