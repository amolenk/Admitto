using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Attendee;

public class DenyAttendeeVisaLetterSettings : TeamEventSettings
{
    [CommandOption("--id")]
    [Description("The id of the attendee to remove")]
    public Guid? Id { get; set; }

    public override ValidationResult Validate()
    {
        return Id is null ? ValidationErrors.IdMissing : base.Validate();
    }
}

public class DenyAttendeeVisaLetterCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<DenyAttendeeVisaLetterSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, DenyAttendeeVisaLetterSettings settings)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        if (!AnsiConsoleExt.Confirm("Deny visa letter (registration will be canceled)?"))
        {
            return 0;
        }
        
        var response = await apiService.CallApiAsync(async client =>
            await client.Teams[teamSlug].Events[eventSlug].Attendees[settings.Id!.Value].DenyVisa.PostAsync());
        if (response is null) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage("Successfully cancelled registration.");
        return 0;
    }
}