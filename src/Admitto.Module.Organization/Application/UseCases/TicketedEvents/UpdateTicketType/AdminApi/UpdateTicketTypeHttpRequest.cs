namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketType.AdminApi;

/// <summary>
/// Request body for updating a ticket type.
/// All fields are required. Capacity null means unlimited.
/// </summary>
public sealed record UpdateTicketTypeHttpRequest(
    string Name,
    int? Capacity,
    bool IsSelfServiceAvailable,
    uint? ExpectedVersion)
{
    internal UpdateTicketTypeCommand ToCommand(Guid teamId, Guid eventId, string ticketTypeSlug) =>
        new(
            teamId,
            eventId,
            ticketTypeSlug,
            Name,
            Capacity,
            IsSelfServiceAvailable,
            ExpectedVersion);
}
