using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Abstractions;

public interface ITicketedEventRepository
{
    ValueTask<IAggregateWithEtag<TicketedEvent>?> FindByIdAsync(Guid ticketedEventId);

    ValueTask<IAggregateWithEtag<TicketedEvent>> GetByIdAsync(Guid ticketedEventId);

    ValueTask SaveChangesAsync(
        TicketedEvent ticketedEvent,
        string? etag = null,
        IEnumerable<OutboxMessage>? outboxMessages = null,
        ICommand? processedCommand = null);
}
