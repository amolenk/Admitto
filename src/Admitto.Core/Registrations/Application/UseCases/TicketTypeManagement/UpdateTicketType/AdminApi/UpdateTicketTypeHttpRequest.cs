namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketTypeManagement.UpdateTicketType.AdminApi;

public sealed record UpdateTicketTypeHttpRequest(
    string? Name = null,
    int? MaxCapacity = null,
    bool? SelfServiceEnabled = null)
{
    internal UpdateTicketTypeCommand ToCommand(Guid eventId, Guid ticketTypeId) => new(
        eventId,
        ticketTypeId,
        Name,
        MaxCapacity,
        SelfServiceEnabled);
}
