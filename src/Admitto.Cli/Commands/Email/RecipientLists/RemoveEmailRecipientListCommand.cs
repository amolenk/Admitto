using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.RecipientLists;

public class RemoveEmailRecipientListSettings : TeamEventSettings
{
    [CommandOption("--name")]
    [Description("The name of the list")]
    public string? ListName { get; init; }

    public override ValidationResult Validate()
    {
        if (ListName is null)
        {
            return ValidationErrors.NameMissing;
        }

        return base.Validate();
    }
}

public class RemoveEmailRecipientListCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<RemoveEmailRecipientListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RemoveEmailRecipientListSettings settings, CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].EmailRecipientLists[settings.ListName].DeleteAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage(
            $"Successfully removed {settings.ListName} recipient list.");
        return 0;
    }
}