using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public static class CreateTicketedEventEndpoint
{
    public static RouteGroupBuilder MapCreateTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateTicketedEvent)
            .WithName(nameof(CreateTicketedEvent))
            .RequireAuthorization(policy => policy.RequireCanCreateEvent());

        return group;
    }

    private static async ValueTask<Created> CreateTicketedEvent(
        string teamSlug,
        CreateTicketedEventRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);

        var newEvent = TicketedEvent.Create(
            teamId,
            request.Slug,
            request.Name,
            request.Website,
            request.StartTime.ToUniversalTime(),
            request.EndTime.ToUniversalTime(),
            request.BaseUrl);

        context.TicketedEvents.Add(newEvent);

        var registrationPolicy = newEvent.RegistrationPolicy;

        // Create a separate availability entity based on the registration policy.
        // This gives us a small aggregate to manage availability (hot path).
        // An alternative to creating the availability aggregate here would be to raise a domain event,
        // but that would complicate the flow without much benefit.
        var newEventAvailability = TicketedEventAvailability.Create(
            newEvent.Id,
            newEvent.StartTime - registrationPolicy.OpensBeforeEvent,
            newEvent.StartTime - registrationPolicy.ClosesBeforeEvent,
            registrationPolicy.EmailDomainName);

        context.TicketedEventAvailability.Add(newEventAvailability);
        
        return TypedResults.Created();
    }
}