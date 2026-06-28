using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlists.LeaveWaitlist.PartnerApi;

public static class LeaveWaitlistHttpEndpoint
{
    public static RouteGroupBuilder MapLeaveWaitlist(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/waitlist/{ticketTypeId:guid}", LeaveWaitlist)
            .WithName(nameof(LeaveWaitlist));

        return group;
    }

    private static async ValueTask<Ok> LeaveWaitlist(
        HttpContext httpContext,
        string eventSlug,
        Guid ticketTypeId,
        string email,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<LeaveWaitlistCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var command = new LeaveWaitlistCommand(teamId, eventId.Value, ticketTypeId, email);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
