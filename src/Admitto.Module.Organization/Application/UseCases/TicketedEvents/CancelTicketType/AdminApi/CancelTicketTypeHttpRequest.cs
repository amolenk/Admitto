namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketType.AdminApi;

public sealed record CancelTicketTypeHttpRequest(uint? ExpectedVersion)
{
    internal CancelTicketTypeCommand ToCommand(Guid teamId, Guid eventId, string ticketTypeSlug) =>
        new(teamId, eventId, ticketTypeSlug, ExpectedVersion);
}
