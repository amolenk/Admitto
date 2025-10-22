using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ReconfirmSettings : TeamEventSettings
{
    [CommandOption("--id")] 
    [Description("The id of the attendee")]
    public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        return Id is null ? ValidationErrors.IdMissing : base.Validate();
    }
}

public class ReconfirmAttendeeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<ReconfirmSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ReconfirmSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.Id!.Value].Reconfirm.PostAsync());
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully reconfirmed registration.");
        return 0;
    }
}