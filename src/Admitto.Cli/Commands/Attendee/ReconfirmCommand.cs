namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class ReconfirmSettings : TeamEventSettings
{
    [CommandOption("--id")]
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

public class ReconfirmCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<ReconfirmSettings>(accessTokenProvider, configuration, outputService)
{
    public override async Task<int> ExecuteAsync(CommandContext context, ReconfirmSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);

        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.Id!.Value].Reconfirm.PostAsync());
        if (response is null) return 1;
        
        outputService.WriteSuccesMessage("Successfully reconfirmed registration.");
        return 0;
    }
}