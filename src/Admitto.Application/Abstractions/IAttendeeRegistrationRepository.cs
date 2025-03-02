using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.Abstractions;

public interface IAttendeeRegistrationRepository
{
    ValueTask<IAggregateWithEtag<AttendeeRegistration>?> FindByIdAsync(Guid attendeeRegistrationId);

    ValueTask<IAggregateWithEtag<AttendeeRegistration>> GetByIdAsync(Guid attendeeRegistrationId);

    ValueTask<IAggregateWithEtag<AttendeeRegistration>> GetOrAddAsync(Guid attendeeRegistrationId, 
        Func<AttendeeRegistration> createAttendeeRegistration);

    ValueTask SaveChangesAsync(
        AttendeeRegistration attendeeRegistration,
        string? etag = null,
        IEnumerable<OutboxMessage>? outboxMessages = null,
        ICommand? handledCommand = null);

    ValueTask DeleteAsync(Guid registrationId);
}
