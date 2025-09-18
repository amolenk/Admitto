using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

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
            request.BaseUrl, 
            request.StartsAt.ToUniversalTime(),
            request.EndsAt.ToUniversalTime(),
            (request.AdditionalDetailSchemas ?? []).Select(
                ads => new AdditionalDetailSchema(ads.Name, ads.MaxLength, ads.IsRequired)));

        context.TicketedEvents.Add(newEvent);
        
        return TypedResults.Created();
    }
}