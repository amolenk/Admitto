namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.RequestTicketedEventCreation.AdminApi;

/// <summary>
/// HTTP request body for the request-event-creation endpoint.
/// </summary>
public sealed record RequestTicketedEventCreationHttpRequest(
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone)
{
    internal RequestTicketedEventCreationCommand ToCommand(Guid teamId, Guid requesterId) =>
        new(teamId, requesterId, Slug, Name, WebsiteUrl, BaseUrl, StartsAt, EndsAt, TimeZone);
}
