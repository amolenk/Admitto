using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.CancelRegistration.AdminApi;

public static class CancelRegistrationHttpEndpoint
{
    public static RouteGroupBuilder MapCancelRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/registrations/{registrationId:guid}/cancel", CancelRegistration)
            .WithName(nameof(CancelRegistration))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> CancelRegistration(
        Guid registrationId,
        string teamSlug,
        string eventSlug,
        CancelRegistrationHttpRequest request,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var reason = Enum.Parse<CancellationReason>(request.Reason!);

        var command = new CancelRegistrationCommand(
            RegistrationId.From(registrationId),
            TicketedEventId.From(scope.EventId!.Value),
            reason);

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
