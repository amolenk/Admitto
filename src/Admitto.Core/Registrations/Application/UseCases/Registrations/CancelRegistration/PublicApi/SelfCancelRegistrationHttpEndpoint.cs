using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration.PublicApi;

public static class SelfCancelRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapSelfCancelRegistration(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations/{registrationId:guid}/cancel", HandleAsync)
            .WithName(nameof(SelfCancelRegistrationHttpEndpoint));

        return group;
    }

    private static async ValueTask<IResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var command = new CancelRegistrationCommand(
            registrationId,
            eventId,
            CancellationReason.AttendeeRequest);

        await mediator.SendAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}
