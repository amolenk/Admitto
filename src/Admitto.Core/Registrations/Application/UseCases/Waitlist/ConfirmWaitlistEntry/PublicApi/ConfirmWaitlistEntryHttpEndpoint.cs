using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Waitlist.ConfirmWaitlistEntry.PublicApi;

public static class ConfirmWaitlistEntryHttpEndpoint
{
    public static RouteGroupBuilder MapConfirmWaitlistEntry(this RouteGroupBuilder group)
    {
        group
            .MapPost("/waitlist/{ticketTypeId:guid}/confirm", HandleAsync)
            .WithName(nameof(ConfirmWaitlistEntryHttpEndpoint));

        return group;
    }

    private static async ValueTask<Ok> HandleAsync(
        Guid eventId,
        Guid ticketTypeId,
        string token,
        ICommandHandler<ConfirmWaitlistEntryCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmWaitlistEntryCommand(eventId, ticketTypeId, token);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
