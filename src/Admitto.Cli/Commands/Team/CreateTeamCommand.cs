namespace Amolenk.Admitto.Cli.Commands.Team;

public class CreateTeamSettings : CommandSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the team to create (e.g. 'awesome-team')")]
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

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationErrors.TeamNameMissing;
        }

        if (string.IsNullOrWhiteSpace(TeamSlug))
        {
            return ValidationErrors.TeamSlugMissing;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            return ValidationErrors.EmailMissing;
        }

        if (string.IsNullOrWhiteSpace(EmailServiceConnectionString))
        {
            return ValidationErrors.EmailServiceConnectionStringMissing;
        }

        return base.Validate();
    }
}

public class CreateTeamCommand(OutputService outputService, IApiService apiService) 
    : AsyncCommand<CreateTeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateTeamSettings settings)
    {
        var request = new CreateTeamRequest()
        {
            Name = settings.Name!,
            Slug = settings.TeamSlug!.Kebaberize(),
            Email = settings.Email!,
            EmailServiceConnectionString = settings.EmailServiceConnectionString!
        };

        var succes = await apiService.CallApiAsync(async client => await client.Teams.PostAsync(request));
        if (!succes) return 1;

        outputService.WriteSuccesMessage($"Successfully created team {request.Name}.");
        return 0;
    }
}