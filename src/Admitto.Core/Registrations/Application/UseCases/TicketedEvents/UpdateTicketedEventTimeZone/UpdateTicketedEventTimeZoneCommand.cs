using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.UpdateTicketedEventTimeZone;

internal sealed record UpdateTicketedEventTimeZoneCommand(
    Guid EventId,
    Guid TeamId,
    uint? ExpectedVersion,
    string TimeZone) : Command;
