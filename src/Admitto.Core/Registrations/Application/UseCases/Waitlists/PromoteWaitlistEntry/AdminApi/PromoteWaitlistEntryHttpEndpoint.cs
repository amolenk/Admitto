using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.PromoteWaitlistEntry.AdminApi;

public static class PromoteWaitlistEntryHttpEndpoint
{
    public static RouteGroupBuilder MapPromoteWaitlistEntry(this RouteGroupBuilder group)
    {
        group
            .MapPost("/waitlist/{entryId:guid}/promote", PromoteWaitlistEntry)
            .WithName(nameof(PromoteWaitlistEntry))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<PromoteWaitlistEntryHttpResponse>> PromoteWaitlistEntry(
        Guid teamId,
        Guid eventId,
        Guid ticketTypeId,
        Guid entryId,
        ICommandHandler<PromoteWaitlistEntryCommand, Guid> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new PromoteWaitlistEntryCommand(eventId, teamId, ticketTypeId, entryId);

        var couponId = await handler.HandleAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamId}/events/{eventId}/coupons/{couponId}",
            new PromoteWaitlistEntryHttpResponse(couponId));
    }
}
