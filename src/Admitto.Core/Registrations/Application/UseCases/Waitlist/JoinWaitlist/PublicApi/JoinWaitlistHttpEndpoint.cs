using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.JoinWaitlist.PublicApi;

public static class JoinWaitlistHttpEndpoint
{
    public static RouteGroupBuilder MapJoinWaitlist(this RouteGroupBuilder group)
    {
        group
            .MapPost("/waitlist/{ticketTypeId:guid}", HandleAsync)
            .WithName(nameof(JoinWaitlistHttpEndpoint));

        return group;
    }

    private static async ValueTask<Accepted> HandleAsync(
        Guid teamId,
        Guid eventId,
        Guid ticketTypeId,
        JoinWaitlistHttpRequest request,
        ICommandHandler<JoinWaitlistCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new JoinWaitlistCommand(
            teamId,
            eventId,
            ticketTypeId,
            request.Email);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Accepted((string?)null);
    }
}
