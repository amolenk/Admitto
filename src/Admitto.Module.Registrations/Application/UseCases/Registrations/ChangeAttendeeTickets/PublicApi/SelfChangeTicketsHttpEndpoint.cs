using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

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
        string teamSlug,
        string eventSlug,
        Guid registrationId,
        SelfChangeTicketsHttpRequest request,
        HttpRequest httpRequest,
        IOrganizationFacade facade,
        ITicketedEventIdLookup ticketedEventIdLookup,
        IVerificationTokenService verificationTokenService,
        IMediator mediator,
        [FromKeyedServices(RegistrationsModule.Key)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var teamId = await facade.GetTeamIdAsync(teamSlug, cancellationToken);
        var eventId = await ticketedEventIdLookup.GetTicketedEventIdAsync(teamId, eventSlug, cancellationToken);

        var bearerToken = ExtractBearerToken(httpRequest);
        if (bearerToken is null)
            return Results.Unauthorized();

        var claims = verificationTokenService.Validate(bearerToken, TicketedEventId.From(eventId));
        if (claims is null)
            return Results.Unauthorized();

        var command = new ChangeAttendeeTicketsCommand(
            TicketedEventId.From(eventId),
            RegistrationId.From(registrationId),
            request.TicketTypeSlugs ?? [],
            ChangeMode.SelfService);

        await mediator.SendAsync(command, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Results.Ok();
    }

    private static string? ExtractBearerToken(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        if (authHeader is null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        return authHeader["Bearer ".Length..].Trim();
    }
}
