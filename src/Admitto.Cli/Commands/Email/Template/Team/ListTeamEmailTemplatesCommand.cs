namespace Amolenk.Admitto.Cli.Commands.Email.Template.Team;

public class ListTeamEmailTemplatesCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<TeamSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, TeamSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var response = await CallApiAsync(async client => await client.Teams[teamSlug].EmailTemplates.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Email type");
        table.AddColumn("Status");

        foreach (var emailTemplate in response.EmailTemplates ?? [])
        {
            var status = emailTemplate.IsCustom ?? false ? "Custom" : "[grey]Default[/]"; 
            
            table.AddRow(emailTemplate.Type!, status);
        }
        
        outputService.Write(table);
        return 0;
    }
}
