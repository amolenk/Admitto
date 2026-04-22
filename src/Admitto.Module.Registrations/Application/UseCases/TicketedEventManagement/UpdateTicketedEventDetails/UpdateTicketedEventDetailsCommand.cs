using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;

internal sealed record UpdateTicketedEventDetailsCommand(
    TicketedEventId EventId,
    uint? ExpectedVersion,
    DisplayName Name,
    AbsoluteUrl WebsiteUrl,
    AbsoluteUrl BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt) : Command;
