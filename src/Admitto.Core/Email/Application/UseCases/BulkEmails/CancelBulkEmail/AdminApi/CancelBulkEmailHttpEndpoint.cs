using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CancelBulkEmail.AdminApi;

public static class CancelBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapCancelBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{bulkEmailJobId:guid}/cancel", CancelBulkEmail)
            .WithName("CancelBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Accepted> CancelBulkEmail(
        Guid teamId,
        Guid eventId,
        Guid bulkEmailJobId,
        ICommandHandler<CancelBulkEmailCommand> handler,
        [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        await handler.HandleAsync(new CancelBulkEmailCommand(bulkEmailJobId, eventId, teamId), ct);
        await unitOfWork.SaveChangesAsync(ct);
        return TypedResults.Accepted((string?)null);
    }
}
