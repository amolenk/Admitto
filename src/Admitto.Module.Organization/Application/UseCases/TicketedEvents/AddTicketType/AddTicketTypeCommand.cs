using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEvents.AddTicketType;

internal sealed record AddTicketTypeCommand(
    Guid TeamId,
    Guid EventId,
    string Slug,
    string Name,
    bool IsSelfService,
    bool IsSelfServiceAvailable,
    string[] TimeSlots,
    int? Capacity,
    uint? ExpectedVersion) : Command;
