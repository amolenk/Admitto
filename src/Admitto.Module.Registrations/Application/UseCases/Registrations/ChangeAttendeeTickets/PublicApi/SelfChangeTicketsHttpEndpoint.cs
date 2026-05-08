using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.PublicApi;

public static class SelfChangeTicketsHttpEndpoint
{
    public static RouteGroupBuilder MapSelfChangeTickets(this RouteGroupBuilder group)
    {
        group.MapPut("/registrations/{registrationId:guid}/tickets", HandleAsync)
            .WithName(nameof(SelfChangeTicketsHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        SelfChangeTicketsHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new ChangeAttendeeTicketsCommand(
            eventId,
            registrationId,
            request.TicketTypeSlugs ?? [],
            ChangeMode.SelfService);

        await mediator.SendAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}
