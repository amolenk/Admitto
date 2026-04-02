using Amolenk.Admitto.Module.Organization.Application.Mapping;
using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamMembershipManagement.ListTeamMembers;

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
