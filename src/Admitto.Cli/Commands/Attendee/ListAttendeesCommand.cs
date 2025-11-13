using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ListAttendeesCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TeamEventSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email");
        table.AddColumn("Name");
        table.AddColumn("Status");
        table.AddColumn("Last updated");

        foreach (var attendee in response.Attendees!.OrderBy(r => r.LastChangedAt!.Value))
        {
            table.AddRow(
                attendee.Email!,
                $"{attendee.FirstName} {attendee.LastName}",
                attendee.Status!.Value.Format(),
                attendee.LastChangedAt!.Value.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}