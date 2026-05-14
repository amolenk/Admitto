using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TeamManagement.GetTeam;

internal class GetTeamHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamQuery, TeamDto>
{
    public async ValueTask<TeamDto> HandleAsync(
        GetTeamQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(query.TeamId);

        return await writeStore.Teams
                   .AsNoTracking()
                   .Where(t => t.Id == teamId)
                   .Select(t => new TeamDto(
                       t.Id.Value,
                       t.Name.Value,
                       t.Version))
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new BusinessRuleViolationException(NotFoundError.Create<Team>(query.TeamId));
    }
}