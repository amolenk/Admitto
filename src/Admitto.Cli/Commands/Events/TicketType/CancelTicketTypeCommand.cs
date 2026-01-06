using Amolenk.Admitto.Cli.Common;

namespace Amolenk.Admitto.Cli.Commands.Events.TicketType;

public class CancelTicketTypeSettings : TeamEventSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the ticket type")]
    public string? Slug { get; set; }
 
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Slug))
        {
            return ValidationErrors.TicketTypeSlugMissing;
        }

        return base.Validate();
    }
}

public class CancelTicketTypeCommand(IApiService apiService, IConfigService configService)
    : AsyncCommand<CancelTicketTypeSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CancelTicketTypeSettings settings, CancellationToken cancellationToken)
    {
        AnsiConsoleExt.WriteWarningMessage("Canceling a ticket type is irreversible and will automatically change or cancel all current registrations.");
        AnsiConsoleExt.WriteWarningMessage("You must inform ticket holders manually before proceeding.");

        var verifySlug = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the ticket type slug to reconfirm:"));

        if (verifySlug != settings.Slug)
        {
            AnsiConsoleExt.WriteErrorMessage("Ticket type slug does not match. Aborting.");
            return 1;
        }
        
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);
        
        var result = await apiService.CallApiAsync(
            async client => await client.Teams[teamSlug].Events[eventSlug].TicketTypes[settings.Slug].DeleteAsync());
        if (result is null) return 1;
        
        AnsiConsoleExt.WriteSuccesMessage($"Successfully canceled ticket type. Current registrations will be adjusted.");
        return 0;
    }
}