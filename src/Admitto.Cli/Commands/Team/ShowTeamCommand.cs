using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Team;

public class ShowTeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    [Description("The slug of the team to show")]
    public string? TeamSlug { get; set; }
}

public class ShowTeamCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ShowTeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ShowTeamSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        
        var response = await apiService.CallApiAsync(async client => await client.Teams[teamSlug].GetAsync());
        if (response is null) return 1;
        
        AnsiConsole.Write(new JsonText(JsonSerializer.Serialize(response)));
        return 0;
    }
}