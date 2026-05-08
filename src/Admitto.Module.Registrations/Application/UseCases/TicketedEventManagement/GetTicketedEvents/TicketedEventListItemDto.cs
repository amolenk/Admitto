using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

public sealed record TicketedEventListItemDto(
    Guid Id,
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone,
    EventLifecycleStatus Status);
