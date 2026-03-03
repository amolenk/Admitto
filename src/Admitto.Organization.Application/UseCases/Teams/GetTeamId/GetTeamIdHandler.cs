using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeamId;

internal class GetTeamIdHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamIdQuery, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        GetTeamIdQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = await writeStore.Teams
            .AsNoTracking()
            .Where(t => t.Slug.Value == query.TeamSlug)
            .Select(t => (Guid?)t.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        return teamId ?? throw new BusinessRuleViolationException(NotFoundError.Create<Team>(query.TeamSlug));
    }
}

