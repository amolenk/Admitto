namespace Amolenk.Admitto.Cli.Commands.Teams;

public class ShowTeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    public string? TeamSlug { get; set; }
}

public class ShowTeamCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService) 
    : ApiCommand<ShowTeamSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ShowTeamSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var response = await CallApiAsync(async client => await client.Teams[teamSlug].GetAsync());
        if (response is null) return 1;
        
        outputService.Write(new JsonText(JsonSerializer.Serialize(response)));
        return 0;
    }
}