using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.Teams.GetTeam;

internal class GetTeamHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamQuery, TeamDto>
{
    public async ValueTask<TeamDto> HandleAsync(
        GetTeamQuery query,
        CancellationToken cancellationToken)
    {
        return await writeStore.Teams
                   .AsNoTracking()
                   .Where(t => t.Id.Value == query.TeamId)
                   .Select(t => new TeamDto(
                       t.Slug.Value,
                       t.Name.Value,
                       t.EmailAddress.Value,
                       t.Version))
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw new BusinessRuleViolationException(NotFoundError.Create<Team>(query.TeamId));
    }
}