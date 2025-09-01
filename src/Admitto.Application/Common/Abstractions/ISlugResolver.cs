namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface ISlugResolver
{
    ValueTask<Guid> ResolveTeamIdAsync(string teamSlug, CancellationToken cancellationToken = default);
    
    ValueTask<Guid> ResolveTicketedEventIdAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default);

    ValueTask<(Guid TeamId, Guid TicketedEventId)> ResolveTeamAndTicketedEventIdsAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default);
}