using Microsoft.Extensions.Configuration;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class ListTeamEmailTemplatesCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration)
    : ApiCommand<TeamSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var response = await CallApiAsync(async client => await client.Teams[teamSlug].Email.Templates.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email type");
        table.AddColumn("Status");

        foreach (var typeName in Enum.GetNames<EmailType>().OrderBy(x => x))
        {
            var type = Enum.Parse<EmailType>(typeName);
            
            var emailTemplate = response.EmailTemplates!.FirstOrDefault(t => t.Type == type);

            var status = emailTemplate is null ? "[grey]Default[/]" : "Customized"; 
            
            table.AddRow(typeName, status);
        }
        
        AnsiConsole.Write(table);
        return 0;
    }
}
