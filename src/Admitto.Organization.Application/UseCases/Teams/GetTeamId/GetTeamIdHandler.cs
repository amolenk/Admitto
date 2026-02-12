// using Amolenk.Admitto.Organization.Application.Persistence;
// using Amolenk.Admitto.Organization.Domain.Entities;
// using Amolenk.Admitto.Shared.Application.Messaging;
// using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
// using Amolenk.Admitto.Shared.Kernel.ValueObjects;
//
// namespace Amolenk.Admitto.Organization.Application.UseCases.Teams.GetTeamId;
//
// internal class GetTeamIdHandler(IOrganizationWriteStore writeStore)
//     : IQueryHandler<GetTeamIdQuery, TeamId>
// {
//     public async ValueTask<TeamId> HandleAsync(
//         GetTeamIdQuery query,
//         CancellationToken cancellationToken)
//     {
//         var teamId = await writeStore.Teams
//             .AsNoTracking()
//             .Where(t => t.Slug == query.TeamSlug)
//             .Select(t => (TeamId?)t.Id)
//             .FirstOrDefaultAsync(cancellationToken);
//
//         return teamId ?? throw new BusinessRuleViolationException(NotFoundError.Create<Team>(query.TeamSlug));
//     }
// }