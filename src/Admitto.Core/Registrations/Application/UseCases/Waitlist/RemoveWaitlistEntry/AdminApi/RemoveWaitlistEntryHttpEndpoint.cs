using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.RemoveWaitlistEntry.AdminApi;

public static class RemoveWaitlistEntryHttpEndpoint
{
    public static RouteGroupBuilder MapRemoveWaitlistEntry(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/waitlist/{entryId:guid}", RemoveWaitlistEntry)
            .WithName(nameof(RemoveWaitlistEntry))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> RemoveWaitlistEntry(
        Guid teamId,
        Guid eventId,
        Guid ticketTypeId,
        Guid entryId,
        ICommandHandler<RemoveWaitlistEntryCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new RemoveWaitlistEntryCommand(eventId, ticketTypeId, entryId);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
