using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;

public static class CancelBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapCancelBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{bulkEmailJobId:guid}/cancel", async (
                Guid teamId,
                Guid eventId,
                Guid bulkEmailJobId,
                IMediator mediator,
                [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                await mediator.SendAsync(
                    new CancelBulkEmailCommand(bulkEmailJobId), ct);

                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.Accepted((string?)null);
            })
            .WithName("CancelBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
