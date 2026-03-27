namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.CreateTicketedEvent.AdminApi;

public sealed record CreateTicketedEventHttpRequest(
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt)
{
    internal CreateTicketedEventCommand ToCommand(Guid teamId) =>
        new(
            teamId,
            Slug,
            Name,
            WebsiteUrl,
            BaseUrl,
            StartsAt.ToUniversalTime(),
            EndsAt.ToUniversalTime());
}
