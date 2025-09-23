namespace Amolenk.Admitto.Cli.Commands.Team;

public class UpdateTeamSettings : CommandSettings
{
    [CommandOption("-t|--team")]
    [Description("Slug of the team")]
    public string? TeamSlug { get; set; }

    [CommandOption("-n|--name")]
    [Description("The name of the team")]
    public string? Name { get; init; }

    [CommandOption("--email")]
    [Description("The email address of the team")]
    public string? Email { get; init; }

    [CommandOption("--emailServiceConnectionString")]
    [Description("The connection string of the SMTP service to use for sending emails")]
    public string? EmailServiceConnectionString { get; init; }
}

public class UpdateTeamCommand(InputService inputService, OutputService outputService, IApiService apiService, IConfiguration configuration) 
    : AsyncCommand<UpdateTeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateTeamSettings settings)
    {
        var teamSlug = settings.TeamSlug ?? configuration[ConfigSettings.DefaultTeamSetting];
        var request = CreateRequest(settings);

        var response = await apiService.CallApiAsync(async client => await client.Teams[teamSlug].PatchAsync(request));
        if (response is null) return 1;

        outputService.WriteSuccesMessage($"Successfully updated team.");
        return 0;
    }

    private UpdateTeamRequest CreateRequest(UpdateTeamSettings settings)
    {
        return new UpdateTeamRequest()
        {
            Name = settings.Name,
            Email = settings.Email,
            EmailServiceConnectionString = settings.EmailServiceConnectionString
        };
    }

    private string GetEmailServiceConnectionString()
    {
        var host = inputService.GetString("SMTP host");
        var port = inputService.GetPort("SMTP port", 587);
        var username = inputService.GetString("SMTP username", allowEmpty: true);
        var password = inputService.GetString("SMTP password", allowEmpty: true, isSecret: true);

        return username is not null
            ? $"host={host};port={port};username={username};password={password}"
            : $"host={host};port={port}";
    }
}