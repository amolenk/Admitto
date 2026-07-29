using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReconfirmRegistration.PartnerApi;

public static class ReconfirmRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapReconfirmRegistration(this RouteGroupBuilder group)
    {
        group.MapPost("/registrations/{registrationId:guid}/reconfirm", ReconfirmRegistration)
            .WithName(nameof(ReconfirmRegistration));

        return group;
    }

    private static async ValueTask<IResult> ReconfirmRegistration(
        HttpContext httpContext,
        string eventSlug,
        Guid registrationId,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<ReconfirmRegistrationCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var command = new ReconfirmRegistrationCommand(
            registrationId,
            eventId.Value,
            teamId);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
