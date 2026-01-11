using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class TicketedEventExtensions
{
    extension(IQueryable<TicketedEvent> ticketedEvents)
    {
        public async ValueTask<TicketedEvent> GetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await ticketedEvents
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return result ?? throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        public ValueTask<TicketedEvent> GetWithoutTrackingAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return ticketedEvents.AsNoTracking().GetAsync(id, cancellationToken);
        }
    }
}