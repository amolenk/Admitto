using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Team;

public class UpdateTeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    [Description("The slug of the team to update")]
    public string? TeamSlug { get; set; }

    [CommandOption("-n|--name")]
    [Description("The team name")]
    public string? Name { get; init; }

    [CommandOption("--email")]
    [Description("The email address where the team can be reached")]
    public string? Email { get; init; }

    [CommandOption("--emailServiceConnectionString")]
    [Description("The connection string of the email service to use for sending emails")]
    public string? EmailServiceConnectionString { get; init; }
}

public class UpdateTeamCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<UpdateTeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateTeamSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);

        var request = new UpdateTeamRequest()
        {
            Name = settings.Name,
            Email = settings.Email,
            EmailServiceConnectionString = settings.EmailServiceConnectionString
        };

        var response = await apiService.CallApiAsync(async client => await client.Teams[teamSlug].PatchAsync(request));
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated team.");
        return 0;
    }
}