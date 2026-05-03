using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ChangeAttendeeTickets.AdminApi;

public static class ChangeAttendeeTicketsHttpEndpoint
{
    public static RouteGroupBuilder MapChangeAttendeeTickets(this RouteGroupBuilder group)
    {
        group
            .MapPut("/registrations/{registrationId:guid}/tickets", ChangeAttendeeTickets)
            .WithName(nameof(ChangeAttendeeTickets))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> ChangeAttendeeTickets(
        Guid registrationId,
        string teamSlug,
        string eventSlug,
        ChangeAttendeeTicketsHttpRequest request,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = new ChangeAttendeeTicketsCommand(
            TicketedEventId.From(scope.EventId!.Value),
            RegistrationId.From(registrationId),
            request.TicketTypeSlugs!,
            ChangeMode.Admin);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
