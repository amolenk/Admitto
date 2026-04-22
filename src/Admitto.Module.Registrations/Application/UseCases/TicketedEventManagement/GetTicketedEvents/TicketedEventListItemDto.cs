using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEvents;

public sealed record TicketedEventListItemDto(
    string Slug,
    string Name,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    EventLifecycleStatus Status);
