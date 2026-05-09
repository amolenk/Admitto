using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;

public static class GetBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapGetBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{bulkEmailJobId:guid}", async (
                Guid teamId,
                Guid eventId,
                Guid bulkEmailJobId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.QueryAsync<GetBulkEmailQuery, BulkEmailJobDetailDto?>(
                    new GetBulkEmailQuery(BulkEmailJobId.From(bulkEmailJobId)), ct);

                if (dto is null)
                    throw new BusinessRuleViolationException(
                        NotFoundError.Create<Domain.Entities.BulkEmailJob>(bulkEmailJobId.ToString()));

                return TypedResults.Ok(dto);
            })
            .WithName("GetBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}

