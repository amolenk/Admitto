using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.TicketType;

public class UpdateTicketTypeSettings : TeamEventSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the ticket type")]
    public string? Slug { get; set; }

    [CommandOption("--maxCapacity")]
    [Description("Maximum available tickets of this type")]
    public int? MaxCapacity { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Slug))
        {
            return ValidationErrors.TicketTypeSlugMissing;
        }

        if (MaxCapacity is null)
        {
            return ValidationErrors.TicketTypeMaxCapacityMissing;
        }

        return base.Validate();
    }
}

public class UpdateTicketTypeCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<UpdateTicketTypeSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        UpdateTicketTypeSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new UpdateTicketTypeRequest()
        {
            MaxCapacity = settings.MaxCapacity!.Value
        };

        var result = await admittoService.SendAsync(client =>
            client.UpdateTicketTypeAsync(teamSlug, eventSlug, settings.Slug, request, cancellationToken));
        if (!result) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully updated ticket type.");
        return 0;
    }
}