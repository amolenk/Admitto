using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.DirectPublicEventLinks;

internal sealed record DirectPublicEventLinksQuery(
    string EventSlug,
    string? ActionPath,
    Guid? RegistrationId,
    string? CouponCode = null)
    : Query<DirectPublicEventLinkDto?>;

public sealed record DirectPublicEventLinkDto(string RedirectUrl);
