using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.Teams.GetTeams;

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
                .OrderBy(t => t.Name)
                .Select(t => new TeamListItemDto(
                    t.Id.Value,
                    t.Name.Value,
                    t.AccentColor.Value,
                    t.Version,
                    CanManageTeamSettings: true,
                    CanCreateEvents: true))
                .ToListAsync(cancellationToken);
        }

        // Non-admin: return only teams the caller is a member of.
        var userId = UserId.From(query.CallerId);

        var memberships = await writeStore.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .SelectMany(u => u.Memberships.Select(m => new { m.TeamId, m.Role }))
            .ToListAsync(cancellationToken);

        var memberTeamIds = memberships.Select(m => m.TeamId).ToList();
        var rolesByTeamId = memberships.ToDictionary(m => m.TeamId, m => m.Role);

        var teams = await writeStore.Teams
            .AsNoTracking()
            .Where(t => t.ArchivedAt == null && memberTeamIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.Id,
                TeamId = t.Id.Value,
                Name = t.Name.Value,
                AccentColor = t.AccentColor.Value,
                t.Version
            })
            .ToListAsync(cancellationToken);

        return teams
            .Select(t =>
            {
                var isOwner = rolesByTeamId[t.Id] == TeamMembershipRole.Owner;

                return new TeamListItemDto(
                    t.TeamId,
                    t.Name,
                    t.AccentColor,
                    t.Version,
                    CanManageTeamSettings: isOwner,
                    CanCreateEvents: isOwner);
            })
            .ToList();
    }
}
