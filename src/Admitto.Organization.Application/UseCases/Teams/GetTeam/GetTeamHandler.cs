using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeam;

internal class GetTeamHandler(IOrganizationWriteStore writeStore)
    : IQueryHandler<GetTeamQuery, TeamDto>
{
    public async ValueTask<TeamDto> HandleAsync(
        GetTeamQuery query,
        CancellationToken cancellationToken)
    {
        return await writeStore.Teams
            .AsNoTracking()
            .Where(t => t.Id == query.TeamId)
            .Select(t => new TeamDto(
                t.Slug.Value,
                t.Name.Value))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BusinessRuleViolationException(Errors.TeamNotFound(query.TeamId));
    }

    private static class Errors
    {
        public static Error TeamNotFound(TeamId teamId) =>
            new(
                "team.not_found",
                "Team could not be found.",
                new Dictionary<string, object?> { ["teamId"] = teamId });
    }
}