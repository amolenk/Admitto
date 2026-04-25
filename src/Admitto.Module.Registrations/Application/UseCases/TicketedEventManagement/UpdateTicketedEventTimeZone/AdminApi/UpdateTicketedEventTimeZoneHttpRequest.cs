using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone.AdminApi;

public sealed record UpdateTicketedEventTimeZoneHttpRequest(
    string TimeZone,
    uint? ExpectedVersion = null)
{
    internal UpdateTicketedEventTimeZoneCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        ExpectedVersion,
        TimeZoneId.From(TimeZone));
}
