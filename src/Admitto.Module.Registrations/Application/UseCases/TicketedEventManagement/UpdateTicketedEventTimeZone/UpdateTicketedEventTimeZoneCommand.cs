using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventTimeZone;

internal sealed record UpdateTicketedEventTimeZoneCommand(
    TicketedEventId EventId,
    uint? ExpectedVersion,
    TimeZoneId TimeZone) : Command;
