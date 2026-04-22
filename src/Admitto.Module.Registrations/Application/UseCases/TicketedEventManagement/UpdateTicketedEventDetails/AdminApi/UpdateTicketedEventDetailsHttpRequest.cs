using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails.AdminApi;

public sealed record UpdateTicketedEventDetailsHttpRequest(
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    uint? ExpectedVersion = null)
{
    internal UpdateTicketedEventDetailsCommand ToCommand(TicketedEventId eventId) => new(
        eventId,
        ExpectedVersion,
        DisplayName.From(Name),
        AbsoluteUrl.From(WebsiteUrl),
        AbsoluteUrl.From(BaseUrl),
        StartsAt,
        EndsAt);
}
