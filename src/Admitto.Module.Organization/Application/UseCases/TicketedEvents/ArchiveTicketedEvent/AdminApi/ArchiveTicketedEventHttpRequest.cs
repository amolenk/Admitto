namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.ArchiveTicketedEvent.AdminApi;

public sealed record ArchiveTicketedEventHttpRequest(uint? ExpectedVersion)
{
    internal ArchiveTicketedEventCommand ToCommand(Guid teamId, Guid eventId) =>
        new(teamId, eventId, ExpectedVersion);
}
