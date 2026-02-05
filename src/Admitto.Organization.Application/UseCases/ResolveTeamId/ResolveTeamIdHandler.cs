using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Organization.Application.UseCases.ResolveTeamId;

internal class ResolveTeamIdHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<ResolveTeamIdQuery, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        ResolveTeamIdQuery query,
        CancellationToken cancellationToken)
    {
        var teamId = await writeStore.Teams
            .AsNoTracking()
            .Where(t => t.Slug == query.TeamSlug)
            .Select(t => (TeamId?)t.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return teamId?.Value ?? throw new BusinessRuleViolationException(Errors.TeamNotFound(query.TeamSlug));
    }
    
    private static class Errors
    {
        public static Error TeamNotFound(TeamSlug slug) =>
            new(
                "team_not_found",
                "Team could not be found.",
                ErrorType.Validation,
                new Dictionary<string, object?> { ["teamSlug"] = slug });
    }
}