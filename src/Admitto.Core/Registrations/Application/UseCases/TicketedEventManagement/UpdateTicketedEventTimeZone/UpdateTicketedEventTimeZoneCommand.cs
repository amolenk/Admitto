using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone;

internal sealed record UpdateTicketedEventTimeZoneCommand(
    Guid EventId,
    uint? ExpectedVersion,
    string TimeZone) : Command;
