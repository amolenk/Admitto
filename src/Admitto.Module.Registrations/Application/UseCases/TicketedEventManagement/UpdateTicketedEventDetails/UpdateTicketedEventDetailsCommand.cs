using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.UpdateTicketedEventDetails;

internal sealed record UpdateTicketedEventDetailsCommand(
    Guid EventId,
    uint? ExpectedVersion,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt) : Command;
