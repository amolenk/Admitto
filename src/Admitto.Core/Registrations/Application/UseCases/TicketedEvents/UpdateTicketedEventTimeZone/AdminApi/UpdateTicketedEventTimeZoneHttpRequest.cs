namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventTimeZone.AdminApi;

public sealed record UpdateTicketedEventTimeZoneHttpRequest(
    string TimeZone,
    uint? ExpectedVersion = null)
{
    internal UpdateTicketedEventTimeZoneCommand ToCommand(Guid eventId, Guid teamId) => new(
        eventId,
        teamId,
        ExpectedVersion,
        TimeZone);
}
