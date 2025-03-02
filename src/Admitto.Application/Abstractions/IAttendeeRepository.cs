// using Amolenk.Admitto.Domain.Entities;
//
// namespace Amolenk.Admitto.Application.Abstractions;
//
// public interface IAttendeeRepository
// {
//     ValueTask<IAggregateWithEtag<Attendee>?> FindByIdAsync(Guid attendeeId);
//
//     ValueTask<IAggregateWithEtag<Attendee>> GetByIdAsync(Guid attendeeId);
//
//     ValueTask<IAggregateWithEtag<Attendee>> GetOrAddAsync(Guid attendeeId, Func<Attendee> createAggregate);
//
//     ValueTask SaveChangesAsync(
//         Attendee attendee,
//         string? etag = null,
//         IEnumerable<OutboxMessage>? outboxMessages = null,
//         ICommand? handledCommand = null);
// }
