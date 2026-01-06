using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Authorization;

public interface ITeamMemberRoleService
{
    public ValueTask<IEnumerable<Guid>> GetTeamsAsync(Guid userId, CancellationToken cancellationToken = default);

    public ValueTask<TeamMemberRole?> GetTeamMemberRoleAsync(
        Guid userId,
        Guid teamId,
        CancellationToken cancellationToken = default);
}