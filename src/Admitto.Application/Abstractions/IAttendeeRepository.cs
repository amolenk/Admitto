using Amolenk.Admitto.Application.Dtos;
using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Abstractions;

public interface IAttendeeRepository
{
    ValueTask<AggregateResult<Attendee>?> GetByIdAsync(Guid attendeeId);

    ValueTask<AggregateResult<Attendee>> GetOrAddAsync(Guid attendeeId, Func<Attendee> createAggregate);

    ValueTask SaveChangesAsync(
        Attendee attendee,
        string? etag = null,
        IEnumerable<OutboxMessage>? outboxMessages = null,
        ICommand? handledCommand = null);
}
