using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeam;

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
                       t.Slug.Value,
                       t.Name.Value,
                       t.EmailAddress.Value,
                       t.Version))
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new BusinessRuleViolationException(NotFoundError.Create<Team>(query.TeamId));
    }
}