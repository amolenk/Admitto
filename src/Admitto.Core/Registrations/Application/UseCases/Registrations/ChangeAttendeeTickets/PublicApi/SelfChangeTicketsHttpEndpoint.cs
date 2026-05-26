using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.PublicApi;

public static class SelfChangeTicketsHttpEndpoint
{
    public static RouteGroupBuilder MapSelfChangeTickets(this RouteGroupBuilder group)
    {
        group.MapPut("/registrations/{registrationId:guid}/tickets", SelfChangeTickets)
            .WithName(nameof(SelfChangeTickets));

        return group;
    }

    private static async ValueTask<IResult> SelfChangeTickets(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        SelfChangeTicketsHttpRequest request,
        ICommandHandler<ChangeAttendeeTicketsCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new ChangeAttendeeTicketsCommand(
            eventId,
            registrationId,
            request.TicketTypeIds ?? [],
            ChangeMode.SelfService);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}
