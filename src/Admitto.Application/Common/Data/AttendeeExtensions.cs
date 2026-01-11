using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Common.Data;

public static class AttendeeExtensions
{
    extension(IQueryable<Attendee> attendees)
    {
        public async ValueTask<Attendee> GetAsync(
            Guid id,
            Guid ticketedEventId,
            CancellationToken cancellationToken = default)
        {
            var result = await attendees
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (result is null || result.TicketedEventId != ticketedEventId)
            {
                throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
            }

            return result;
        }
        
        public ValueTask<Attendee> GetWithoutTrackingAsync(
            Guid id,
            Guid ticketedEventId,
            CancellationToken cancellationToken = default)
        {
            return attendees.AsNoTracking().GetAsync(id, ticketedEventId, cancellationToken);
        }
    }
}