using Amolenk.Admitto.Core.Organization.Application.Mapping;
using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamMemberships.GetTeamMembers;

internal sealed class GetTeamMembersHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamMembersQuery, IReadOnlyList<TeamMemberListItemDto>>
{
    public async ValueTask<IReadOnlyList<TeamMemberListItemDto>> HandleAsync(
        GetTeamMembersQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);

        return await writeStore.Users
            .AsNoTracking()
            .Where(u => u.Memberships.Any(m => m.TeamId == teamId))
            .Select(u => new TeamMemberListItemDto(
                u.EmailAddress.Value,
                u.Memberships.First(m => m.TeamId == teamId).Role.ToDto()))
            .ToListAsync(cancellationToken);
    }
}
