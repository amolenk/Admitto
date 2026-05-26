namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails.AdminApi;

public sealed record UpdateTicketedEventDetailsHttpRequest(
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd,
    uint? ExpectedVersion = null)
{
    internal UpdateTicketedEventDetailsCommand ToCommand(Guid eventId) => new(
        eventId,
        ExpectedVersion,
        Name,
        WebsiteUrl,
        BaseUrl,
        StartsAt,
        EndsAt,
        QuietHoursStart,
        QuietHoursEnd);
}
