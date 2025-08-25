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

        foreach (var typeName in Enum.GetNames<EmailType>().OrderBy(x => x))
        {
            var type = Enum.Parse<EmailType>(typeName);
            
            var emailTemplate = response.EmailTemplates!.FirstOrDefault(t => t.Type == type);

            var status = emailTemplate is null ? "[grey]Default[/]" : 
                emailTemplate.TicketedEventId is null ? "Team customized" : "Event customized"; 
            
            table.AddRow(typeName, status);
        }
        
        AnsiConsole.Write(table);
        return 0;
    }
}
