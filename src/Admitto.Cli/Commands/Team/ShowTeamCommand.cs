namespace Amolenk.Admitto.Cli.Commands.Teams;

public class ShowTeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    public string? TeamSlug { get; set; }
}

public class ShowTeamCommand : ApiCommand<ShowTeamSettings>
{
    public ShowTeamCommand(IAccessTokenProvider accessTokenProvider, IConfiguration configuration) 
        : base(accessTokenProvider, configuration)
    {
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, ShowTeamSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        
        var response = await CallApiAsync(async client => await client.Teams[teamSlug].GetAsync());
        if (response is null) return 1;
        
        AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(response)));
        return 0;
    }
}