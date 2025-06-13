using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IRebacAuthorizationService
{
    ValueTask AddGlobalAdminAsync(Guid userId, CancellationToken cancellationToken = default);

    ValueTask AddTeamRoleAsync(Guid userId, TeamId teamId, TeamMemberRole role, 
        CancellationToken cancellationToken = default);

    ValueTask<bool> CheckAsync(Guid userId, string relation, string objectType, string objectId,
        CancellationToken cancellationToken = default);
}