using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeams;

/// <summary>
/// Implements US-003 (admin: list all active teams) and US-006 (member: list own active teams).
/// Admins receive every non-archived team; non-admins receive only teams they belong to.
/// </summary>
internal sealed class GetTeamsHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamsQuery, IReadOnlyList<TeamListItemDto>>
{
    public async ValueTask<IReadOnlyList<TeamListItemDto>> HandleAsync(
        GetTeamsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.CallerIsAdmin)
        {
            return await writeStore.Teams
                .AsNoTracking()
                .Where(t => t.ArchivedAt == null)
                .Select(t => new TeamListItemDto(                    t.Id.Value,
                    t.Name.Value,
                    t.Version))
                .ToListAsync(cancellationToken);
        }

        // Non-admin: return only teams the caller is a member of.
        var userId = UserId.From(query.CallerId);

        var memberTeamIds = await writeStore.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Memberships.Select(m => m.Id))
            .ToListAsync(cancellationToken);

        return await writeStore.Teams
            .AsNoTracking()
            .Where(t => t.ArchivedAt == null && memberTeamIds.Contains(t.Id))
            .Select(t => new TeamListItemDto(
                t.Id.Value,
                t.Name.Value,
                t.Version))
            .ToListAsync(cancellationToken);
    }
}
