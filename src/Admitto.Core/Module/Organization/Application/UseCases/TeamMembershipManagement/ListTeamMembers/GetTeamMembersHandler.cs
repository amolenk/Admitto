using Amolenk.Admitto.Core.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Core.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

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
            .Where(u => u.Memberships.Any(m => m.Id == teamId))
            .Select(u => new TeamMemberListItemDto(
                u.EmailAddress.Value,
                u.Memberships.First(m => m.Id == teamId).Role.ToDto()))
            .ToListAsync(cancellationToken);
    }
}
