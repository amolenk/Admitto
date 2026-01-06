using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Contributor;

public class RemoveContributorSettings : TeamEventSettings
{
    [CommandOption("--id")] 
    [Description("The id of the contributor to remove")]
    public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        if (Id is null)
        {
            return ValidationErrors.IdMissing;
        }

        return base.Validate();
    }
}

public class RemoveContributorCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<RemoveContributorSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RemoveContributorSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Contributors[settings.Id!.Value]
                .DeleteAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully removed contributor.");
        return 0;
    }
}