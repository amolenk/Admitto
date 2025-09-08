namespace Amolenk.Admitto.Cli.Commands.Team;

public class CreateTeamSettings : CommandSettings
{
    [CommandOption("-s|--slug")]
    [Description("The team ID (e.g. my-cool-event)")]
    public string? TeamSlug { get; init; }

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

public class CreateTeamCommand(IApiService apiService) : AsyncCommand<CreateTeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateTeamSettings settings)
    {
        var request = CreateRequest(settings);

        var succes = await apiService.CallApiAsync(async client => await client.Teams.PostAsync(request));
        if (!succes) return 1;

        OutputService.WriteSuccesMessage($"Successfully created team {request.Name}.");
        return 0;
    }

    private static CreateTeamRequest CreateRequest(CreateTeamSettings settings)
    {
        var name = settings.Name ?? InputService.GetString("Team name");
        var slug = settings.TeamSlug?.Kebaberize() ??
                   InputService.GetString("Team slug", name.Kebaberize(), kebaberize: true);
        var email = settings.Email ?? InputService.GetString("Team email");
        var emailServiceConnectionString = settings.EmailServiceConnectionString ?? GetEmailServiceConnectionString();

        return new CreateTeamRequest()
        {
            Name = name,
            Slug = slug,
            Email = email,
            EmailServiceConnectionString = emailServiceConnectionString
        };
    }

    private static string GetEmailServiceConnectionString()
    {
        var host = InputService.GetString("SMTP host");
        var port = InputService.GetPort("SMTP port", 587);
        var username = InputService.GetString("SMTP username", allowEmpty: true);
        var password = InputService.GetString("SMTP password", allowEmpty: true, isSecret: true);

        return username is not null
            ? $"host={host};port={port};username={username};password={password}"
            : $"host={host};port={port}";
    }
}