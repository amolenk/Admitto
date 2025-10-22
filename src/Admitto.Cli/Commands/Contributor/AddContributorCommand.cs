using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Contributor;

public class AddSettings : TeamEventSettings
{
    [CommandOption("--email")]
    [Description("The email address")]
    public string? Email { get; set; }

    [CommandOption("--firstName")]
    [Description("The first name")]
    public string? FirstName { get; set; } = null!;

    [CommandOption("--lastName")]
    [Description("The last name")]
    public string? LastName { get; set; } = null!;

    [CommandOption("--detail")]
    [Description("Additional contributor information in the format 'Name=Value'")]
    public string[]? AdditionalDetails { get; set; } = null!;

    [CommandOption("--role")]
    [ContributorRoleDescription]
    public ContributorRole?[]? Roles { get; set; } = null!;

    public override ValidationResult Validate()
    {
        if (Email is null)
        {
            return ValidationErrors.EmailMissing;
        }

        if (FirstName is null)
        {
            return ValidationErrors.FirstNameMissing;
        }

        if (LastName is null)
        {
            return ValidationErrors.LastNameMissing;
        }

        if (Roles is null || Roles.Length == 0)
        {
            return ValidationErrors.RoleMissing;
        }

        return base.Validate();
    }
}

public class AddContributorCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<AddSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, AddSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new AddContributorRequest
        {
            Email = settings.Email,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            AdditionalDetails = InputHelper.ParseAdditionalDetails(settings.AdditionalDetails),
            Roles = settings.Roles!.ToList()
        };

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Contributors.PostAsync(request));
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully added contributor (registration ID = '{response.RegistrationId}').");
        return 0;
    }
}