using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.GetTicketedEvents;

public sealed record TicketedEventListItemDto(
    Guid Id,
    string Name,
    string PublicSlug,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone,
    EventLifecycleStatus Status);
