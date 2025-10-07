using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IAuthorizationService
{
    ValueTask<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken = default);

    ValueTask<bool> CanUpdateTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);
    
    ValueTask<bool> CanViewTeamAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);

    ValueTask<bool> CanCreateEventAsync(Guid userId, Guid teamId, CancellationToken cancellationToken = default);
    
    ValueTask<bool> CanUpdateEventAsync(Guid userId, Guid teamId, Guid ticketedEventId,
        CancellationToken cancellationToken = default);

    ValueTask<bool> CanViewEventAsync(Guid userId, Guid teamId, Guid ticketedEventId,
        CancellationToken cancellationToken = default);
    
    ValueTask AddTicketedEventAsync(Guid teamId, Guid ticketedEventId, CancellationToken cancellationToken = default);

    ValueTask AddTeamRoleAsync(Guid userId, Guid teamId, TeamMemberRole role, 
        CancellationToken cancellationToken = default);
    
    ValueTask<IEnumerable<Guid>> GetTeamsAsync(Guid userId, CancellationToken cancellationToken = default);
    
    ValueTask<IEnumerable<Guid>> GetTicketedEventsAsync(Guid userId, Guid teamId,
        CancellationToken cancellationToken = default);
}

