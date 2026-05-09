using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

public sealed record TicketedEventListItemDto(
    Guid Id,
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone,
    EventLifecycleStatus Status);
