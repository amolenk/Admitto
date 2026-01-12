using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Team;

public class CreateTeamSettings : CommandSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the team to create (e.g. 'awesome-team')")]
    public string? TeamSlug { get; init; }

    [CommandOption("-n|--name")]
    [Description("The team name")]
    public string? Name { get; init; }

    [CommandOption("--email")]
    [Description("The email address where the team can be reached")]
    public string? Email { get; init; }

    [CommandOption("--emailServiceConnectionString")]
    [Description("The connection string of the email service to use for sending emails")]
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

public class CreateTeamCommand(IAdmittoService admittoService) 
    : AsyncCommand<CreateTeamSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CreateTeamSettings settings, CancellationToken cancellationToken)
    {
        var request = new CreateTeamRequest()
        {
            Name = settings.Name,
            Slug = settings.TeamSlug!.Kebaberize(),
            Email = settings.Email,
            EmailServiceConnectionString = settings.EmailServiceConnectionString
        };

        var succes = await admittoService.SendAsync(client => client.CreateTeamAsync(request, cancellationToken));
        if (!succes) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully created team {request.Name}.");
        return 0;
    }
}