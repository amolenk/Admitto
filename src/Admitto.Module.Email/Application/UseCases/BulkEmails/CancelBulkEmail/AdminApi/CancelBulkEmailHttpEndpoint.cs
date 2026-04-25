using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;

public static class CancelBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapCancelBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{bulkEmailJobId:guid}/cancel", async (
                Guid bulkEmailJobId,
                IMediator mediator,
                [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                await mediator.SendAsync(
                    new CancelBulkEmailCommand(BulkEmailJobId.From(bulkEmailJobId)), ct);

                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.Accepted((string?)null);
            })
            .WithName("CancelBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
