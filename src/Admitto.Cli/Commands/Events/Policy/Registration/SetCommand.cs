namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Registration;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--opens-before")]
    public TimeSpan? OpensBeforeEvent { get; set; }

    [CommandOption("--closes-before")]
    public TimeSpan? ClosesBeforeEvent { get; set; }
    
    public override ValidationResult Validate()
    {
        if (OpensBeforeEvent is null)
        {
            // TODO
            return ValidationErrors.ReconfirmWindowStartMissing;
        }

        if (ClosesBeforeEvent is null)
        {
            // TODO
            return ValidationErrors.ReconfirmWindowEndMissing;
        }

        return base.Validate();
    }
}

public class SetCommand(
    IAccessTokenProvider accessTokenProvider, 
    IConfiguration configuration,
    OutputService outputService)
    : ApiCommand<SetSettings>(accessTokenProvider, configuration)
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetSettings settings)
    {
        var teamSlug = GetTeamSlug(settings.TeamSlug);
        var eventSlug = GetEventSlug(settings.EventSlug);
        
        var request = new SetRegistrationPolicyRequest
        {
            OpensBeforeEvent = settings.OpensBeforeEvent.ToString(),
            ClosesBeforeEvent = settings.ClosesBeforeEvent.ToString()
        };
        
        var response = await CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Policies.Registration.PutAsync(request));
        if (response is null) return 1;

        outputService.WriteSuccesMessage("Successfully set registration policy.");
        return 0;
    }
}