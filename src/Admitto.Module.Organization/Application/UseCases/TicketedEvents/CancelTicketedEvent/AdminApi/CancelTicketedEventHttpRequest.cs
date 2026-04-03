namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CancelTicketedEvent.AdminApi;

public sealed record CancelTicketedEventHttpRequest(uint? ExpectedVersion)
{
    internal CancelTicketedEventCommand ToCommand(Guid teamId, Guid eventId) =>
        new(teamId, eventId, ExpectedVersion);
}
