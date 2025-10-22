using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class ListTeamEmailTemplatesCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<TeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        
        var response = await apiService.CallApiAsync(async client => await client.Teams[teamSlug].EmailTemplates.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email type");
        table.AddColumn("Status");

        foreach (var emailTemplate in response.EmailTemplates ?? [])
        {
            var status = emailTemplate.IsCustom ?? false ? "Custom" : "[grey]Default[/]"; 
            
            table.AddRow(emailTemplate.Type!, status);
        }
        
        AnsiConsole.Write(table);
        return 0;
    }
}
