using Amolenk.Admitto.Cli.Commands.Events;
using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Email.Bulk;

public class RemoveBulkEmailSettings : TeamEventSettings
{
    [CommandOption("--id")] 
    [Description("The id of the bulk email to remove")]
    public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        return Id is null ? ValidationErrors.IdMissing : base.Validate();
    }
}

public class RemoveBulkEmailCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<RemoveBulkEmailSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RemoveBulkEmailSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Emails.Bulk[settings.Id!.Value].DeleteAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully removed bulk email.");
        return 0;
    }
}