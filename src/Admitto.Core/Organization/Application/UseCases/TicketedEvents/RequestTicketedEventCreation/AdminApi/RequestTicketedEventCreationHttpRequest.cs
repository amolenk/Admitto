namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.RequestTicketedEventCreation.AdminApi;

/// <summary>
/// HTTP request body for the request-event-creation endpoint.
/// </summary>
public sealed record RequestTicketedEventCreationHttpRequest(
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    string PublicSlug,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone)
{
    internal RequestTicketedEventCreationCommand ToCommand(Guid teamId, Guid requesterId) =>
        new(teamId, requesterId, Name, WebsiteUrl, BaseUrl, StartsAt, EndsAt, TimeZone, PublicSlug);
}
