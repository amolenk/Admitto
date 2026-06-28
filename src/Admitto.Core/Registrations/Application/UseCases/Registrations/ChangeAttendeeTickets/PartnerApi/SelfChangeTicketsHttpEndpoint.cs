using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.PartnerApi;

public static class SelfChangeTicketsHttpEndpoint
{
    public static RouteGroupBuilder MapSelfChangeTickets(this RouteGroupBuilder group)
    {
        group.MapPut("/registrations/{registrationId:guid}/tickets", SelfChangeTickets)
            .WithName(nameof(SelfChangeTickets));

        return group;
    }

    private static async ValueTask<IResult> SelfChangeTickets(
        HttpContext httpContext,
        string eventSlug,
        Guid registrationId,
        SelfChangeTicketsHttpRequest request,
        PartnerTicketedEventResolver eventResolver,
        ICommandHandler<ChangeAttendeeTicketsCommand> handler,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);
        var command = new ChangeAttendeeTicketsCommand(
            eventId.Value,
            teamId,
            registrationId,
            request.TicketTypeIds ?? [],
            ChangeMode.SelfService,
            request.WaitlistCouponCode);

        await handler.HandleAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }
}
