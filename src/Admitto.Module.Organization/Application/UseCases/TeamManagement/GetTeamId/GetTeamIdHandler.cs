using Amolenk.Admitto.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TeamManagement.GetTeamId;

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

