using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Events.Policy.Registration;

public class SetSettings : TeamEventSettings
{
    [CommandOption("--opens-before")]
    [Description("The timespan before the event when registration opens")]
    public TimeSpan? OpensBeforeEvent { get; set; }

    [CommandOption("--closes-before")]
    [Description("The timespan before the event when registration closes")]
    public TimeSpan? ClosesBeforeEvent { get; set; }
    
    public override ValidationResult Validate()
    {
        if (OpensBeforeEvent is null)
        {
            return ValidationErrors.OpensBeforeEventMissing;
        }

        if (ClosesBeforeEvent is null)
        {
            return ValidationErrors.ClosesBeforeEventMissing;
        }

        return base.Validate();
    }
}

public class SetRegistrationPolicyCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<SetSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var request = new SetRegistrationPolicyRequest
        {
            OpensBeforeEvent = settings.OpensBeforeEvent.ToString(),
            ClosesBeforeEvent = settings.ClosesBeforeEvent.ToString()
        };
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Policies.Registration.PutAsync(request));
        if (response is null) return 1;

        AnsiConsoleExt.WriteSuccesMessage("Successfully set registration policy.");
        return 0;
    }
}