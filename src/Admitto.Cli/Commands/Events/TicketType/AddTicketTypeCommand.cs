using Amolenk.Admitto.Cli.Api;
using Amolenk.Admitto.Cli.Common;
using Amolenk.Admitto.Cli.Configuration;
using Amolenk.Admitto.Cli.IO;

namespace Amolenk.Admitto.Cli.Commands.Events.TicketType;

public class AddTicketTypeSettings : TeamEventSettings
{
    [CommandOption("-s|--slug")]
    [Description("Slug of the ticket type")]
    public string? Slug { get; set; }

    [CommandOption("-n|--name")]
    [Description("Ticket type name")]
    public string? Name { get; set; } = null!;

    [CommandOption("--slotName")]
    [Description("Name(s) of the slot(s) occupied by this ticket type")]
    public string[]? SlotName { get; set; }

    [CommandOption("--maxCapacity")]
    [Description("Maximum available tickets of this type")]
    public int? MaxCapacity { get; set; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Slug))
        {
            return ValidationErrors.TicketTypeSlugMissing;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationErrors.TicketTypeNameMissing;
        }

        return base.Validate();
    }
}

public class AddTicketTypeCommand(IAdmittoService admittoService, IConfigService configService)
    : AsyncCommand<AddTicketTypeSettings>
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        AddTicketTypeSettings settings,
        CancellationToken cancellationToken)
    {
        var teamSlug = InputHelper.ResolveTeamSlug(settings.TeamSlug, configService);
        var eventSlug = InputHelper.ResolveEventSlug(settings.EventSlug, configService);

        var request = new AddTicketTypeRequest
        {
            Slug = settings.Slug,
            Name = settings.Name,
            SlotNames = (settings.SlotName ?? []).ToList(),
            MaxCapacity = settings.MaxCapacity!.Value
        };

        var succes = await admittoService.SendAsync(client =>
            client.AddTicketTypeAsync(teamSlug, eventSlug, request, cancellationToken));
        if (!succes) return 1;

        AnsiConsoleExt.WriteSuccesMessage($"Successfully added ticket type {settings.Name}.");
        return 0;
    }
}