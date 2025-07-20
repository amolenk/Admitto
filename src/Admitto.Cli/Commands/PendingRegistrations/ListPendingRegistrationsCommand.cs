using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.PendingRegistrations;

public class ListPendingRegistrationsCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : PendingRegistrationCommandBase<PendingRegistrationSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, PendingRegistrationSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client => 
            await client.Teams[teamSlug].Events[eventSlug].Registrations.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Email");
        table.AddColumn("Name");
        table.AddColumn("Status");
        table.AddColumn("Last updated");

        foreach (var registration in response.RegistrationRequests!
                     .OrderByDescending(r => r.LastChangedAt!.Value))
        {
            table.AddRow(
                $"[grey]{registration.RegistrationId}[/]",
                registration.Email!,
                $"{registration.FirstName} {registration.LastName}",
                registration.Status!.Value.Format(),
                registration.LastChangedAt!.Value.Format());
        }

        AnsiConsole.Write(table);
        return 0;
    }
}