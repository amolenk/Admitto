using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public sealed record UpdateTicketTypeHttpRequest(
    string? Name = null,
    int? MaxCapacity = null,
    bool? SelfServiceEnabled = null)
{
    internal UpdateTicketTypeCommand ToCommand(TicketedEventId eventId, string ticketTypeSlug) => new(
        eventId,
        Slug.From(ticketTypeSlug),
        Name is not null ? DisplayName.From(Name) : null,
        MaxCapacity,
        SelfServiceEnabled);
}
