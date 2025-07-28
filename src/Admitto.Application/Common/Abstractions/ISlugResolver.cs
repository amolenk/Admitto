namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface ISlugResolver
{
    ValueTask<Guid> GetTeamIdAsync(string teamSlug, CancellationToken cancellationToken = default);

    ValueTask<Guid> GetTicketedEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default);

    ValueTask<(Guid TeamId, Guid TicketedEventId)> GetTeamAndTicketedEventsIdsAsync(
        string teamSlug,
        string eventSlug,
        CancellationToken cancellationToken = default);
}