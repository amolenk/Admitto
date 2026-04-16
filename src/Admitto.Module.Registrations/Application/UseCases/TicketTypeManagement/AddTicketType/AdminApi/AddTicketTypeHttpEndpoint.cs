using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.AddTicketType.AdminApi;

public static class AddTicketTypeHttpEndpoint
{
    public static RouteGroupBuilder MapAddTicketType(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddTicketType)
            .WithName(nameof(AddTicketType))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<AddTicketTypeHttpResponse>> AddTicketType(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        AddTicketTypeHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var command = request.ToCommand(TicketedEventId.From(scope.EventId!.Value));

        await mediator.SendAsync(command, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/teams/{teamSlug}/events/{eventSlug}/ticket-types/{request.Slug}",
            new AddTicketTypeHttpResponse(request.Slug));
    }
}
