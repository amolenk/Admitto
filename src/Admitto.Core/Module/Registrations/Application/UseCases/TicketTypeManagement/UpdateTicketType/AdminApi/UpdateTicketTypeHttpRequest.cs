namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public sealed record UpdateTicketTypeHttpRequest(
    string? Name = null,
    int? MaxCapacity = null,
    bool? SelfServiceEnabled = null)
{
    internal UpdateTicketTypeCommand ToCommand(Guid eventId, string ticketTypeSlug) => new(
        eventId,
        ticketTypeSlug,
        Name,
        MaxCapacity,
        SelfServiceEnabled);
}
