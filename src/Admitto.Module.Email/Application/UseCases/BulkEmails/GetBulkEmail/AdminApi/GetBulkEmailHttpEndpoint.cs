using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;

public static class GetBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapGetBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{bulkEmailJobId:guid}", async (
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
