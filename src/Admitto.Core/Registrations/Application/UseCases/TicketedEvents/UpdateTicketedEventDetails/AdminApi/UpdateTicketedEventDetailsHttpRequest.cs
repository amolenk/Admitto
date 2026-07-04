namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventDetails.AdminApi;

public sealed record UpdateTicketedEventDetailsHttpRequest(
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    string PublicSlug,
    string TimeZone,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    uint? ExpectedVersion = null)
{
    internal UpdateTicketedEventDetailsCommand ToCommand(Guid eventId, Guid teamId) => new(
        eventId,
        teamId,
        ExpectedVersion,
        Name,
        WebsiteUrl,
        BaseUrl,
        TimeZone,
        StartsAt,
        EndsAt,
        PublicSlug);
}
