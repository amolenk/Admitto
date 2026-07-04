using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePartnerTicketedEvent.PartnerApi;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetPartnerRegistrationDetails.PartnerApi;

public static class GetPartnerRegistrationDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetPartnerRegistrationDetails(this RouteGroupBuilder group)
    {
        group.MapGet("/registrations/{registrationId:guid}", GetPartnerRegistrationDetails)
            .WithName(nameof(GetPartnerRegistrationDetails));

        return group;
    }

    private static async ValueTask<Results<Ok<PartnerRegistrationDetailDto>, NotFound>> GetPartnerRegistrationDetails(
        HttpContext httpContext,
        string eventSlug,
        Guid registrationId,
        PartnerTicketedEventResolver eventResolver,
        IQueryHandler<GetPartnerRegistrationDetailsQuery, PartnerRegistrationDetailDto?> handler,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var eventId = await eventResolver.ResolveAsync(TeamId.From(teamId), eventSlug, cancellationToken);

        var query = new GetPartnerRegistrationDetailsQuery(
            TeamId: teamId,
            EventId: eventId,
            RegistrationId: registrationId);

        var result = await handler.HandleAsync(query, cancellationToken);

        if (result is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(result);
    }
}
