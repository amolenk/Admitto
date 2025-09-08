using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IAuthorizationService
{
    ValueTask<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default);

    ValueTask<bool> CanUpdateTeamAsync(Guid userId, string teamSlug, CancellationToken cancellationToken = default);
    
    ValueTask<bool> CanViewTeamAsync(Guid userId, string teamSlug, CancellationToken cancellationToken = default);

    ValueTask<bool> CanCreateEventAsync(Guid userId, string teamSlug, CancellationToken cancellationToken = default);
    
    ValueTask<bool> CanUpdateEventAsync(Guid userId, string teamSlug, string eventSlug,
        CancellationToken cancellationToken = default);

    ValueTask<bool> CanViewEventAsync(Guid userId, string teamSlug, string eventSlug,
        CancellationToken cancellationToken = default);
    
    ValueTask AddTicketedEventAsync(string teamSlug, string eventSlug, CancellationToken cancellationToken = default);

    ValueTask AddTeamRoleAsync(Guid userId, string teamSlug, TeamMemberRole role, 
        CancellationToken cancellationToken = default);
    
    ValueTask<IEnumerable<string>> GetTeamsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    ValueTask<IEnumerable<string>> GetTicketedEventsAsync(Guid userId, string teamSlug,
        CancellationToken cancellationToken = default);
}

