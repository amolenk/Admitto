namespace Amolenk.Admitto.Cli.Commands.Teams;

public class ListTeamsCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<PagingSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, PagingSettings settings)
    {
        var response = await CallApiAsync(async client => await client.Teams.GetAsync());
        if (response is null) return 1;

        var table = new Table();
        table.AddColumn("Slug");
        table.AddColumn("Name");
        table.AddColumn("Email");

        foreach (var team in response.Teams ?? [])
        {
            table.AddRow(team.Slug!, team.Name!, team.Email!);
        }

        outputService.Write(table);
        return 0;
    }
}
