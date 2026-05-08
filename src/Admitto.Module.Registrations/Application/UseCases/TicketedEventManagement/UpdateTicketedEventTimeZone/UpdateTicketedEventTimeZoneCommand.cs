using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone;

internal sealed record UpdateTicketedEventTimeZoneCommand(
    Guid EventId,
    uint? ExpectedVersion,
    string TimeZone) : Command;
