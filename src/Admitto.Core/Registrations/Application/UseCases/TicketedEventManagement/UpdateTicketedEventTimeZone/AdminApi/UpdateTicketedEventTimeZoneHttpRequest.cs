namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone.AdminApi;

public sealed record UpdateTicketedEventTimeZoneHttpRequest(
    string TimeZone,
    uint? ExpectedVersion = null)
{
    internal UpdateTicketedEventTimeZoneCommand ToCommand(Guid eventId) => new(
        eventId,
        ExpectedVersion,
        TimeZone);
}
