namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.UpdateTicketedEvent.AdminApi;

public sealed record UpdateTicketedEventHttpRequest(
    string? Name,
    string? WebsiteUrl,
    string? BaseUrl,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    uint? ExpectedVersion)
{
    internal UpdateTicketedEventCommand ToCommand(Guid teamId, Guid eventId) =>
        new(
            teamId,
            eventId,
            Name,
            WebsiteUrl,
            BaseUrl,
            StartsAt?.ToUniversalTime(),
            EndsAt?.ToUniversalTime(),
            ExpectedVersion);
}
