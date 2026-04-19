using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.CloseRegistration.AdminApi;

public static class CloseRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapCloseRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/registration/close", CloseRegistration)
            .WithName(nameof(CloseRegistration))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> CloseRegistration(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = new CloseRegistrationCommand(TicketedEventId.From(scope.EventId!.Value));

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
