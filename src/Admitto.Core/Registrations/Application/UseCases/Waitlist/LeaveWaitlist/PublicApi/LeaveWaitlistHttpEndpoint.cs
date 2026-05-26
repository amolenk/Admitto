using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.LeaveWaitlist.PublicApi;

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
        Guid eventId,
        Guid ticketTypeId,
        string email,
        ICommandHandler<LeaveWaitlistCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new LeaveWaitlistCommand(eventId, ticketTypeId, email);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
