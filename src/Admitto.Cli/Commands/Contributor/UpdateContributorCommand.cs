using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Contributor;

public class UpdateContributorSettings : TeamEventSettings
{
    [CommandOption("--id")] 
    [Description("The id of the contributor to update")]
    public Guid? Id { get; set; }

    [CommandOption("--firstName")]
    [Description("The first name")]
    public string? FirstName { get; set; } = null!;

    [CommandOption("--lastName")]
    [Description("The last name")]
    public string? LastName { get; set; } = null!;

    [CommandOption("--additionalDetail")]
    [Description("Additional contributor information in the format 'Name=Value'")]
    public string[]? AdditionalDetails { get; set; } = null!;

    [CommandOption("--role")]
    [ContributorRoleDescription]
    public ContributorRole?[]? Roles { get; set; } = null!;

    public override ValidationResult Validate()
    {
        return Id is null ? ValidationErrors.IdMissing : base.Validate();
    }
}

public class UpdateContributorCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<UpdateContributorSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, UpdateContributorSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new UpdateContributorRequest
        {
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            AdditionalDetails = InputHelper.ParseAdditionalDetails(settings.AdditionalDetails),
            Roles = settings.Roles?.ToList()
        };

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Contributors[settings.Id!.Value].PatchAsync(request));
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully updated contributor.");
        return 0;
    }
}