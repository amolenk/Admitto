using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Event;

public class ListEventEmailTemplatesCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<TeamEventSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamEventSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].EmailTemplates.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email type");
        table.AddColumn("Status");

        foreach (var emailTemplate in response.EmailTemplates ?? [])
        {
            var status = emailTemplate.IsCustom ?? false 
                ? emailTemplate.TicketedEventId is null ? "Custom (team-level)" : "Custom (event-level)" : "[grey]Default[/]"; 
            
            table.AddRow(emailTemplate.Type!, status);
        }
        
        AnsiConsole.Write(table);
        return 0;
    }
}
