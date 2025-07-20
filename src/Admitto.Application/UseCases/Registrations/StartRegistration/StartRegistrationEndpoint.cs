using Amolenk.Admitto.Application.Common.Data;
using Amolenk.Admitto.Application.Common.Validation;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

public static class StartRegistrationEndpoint
{
    public static RouteGroupBuilder MapStartRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", StartRegistration)
            .WithName(nameof(StartRegistration));

        return group;
    }

    private static async ValueTask<Accepted<StartRegistrationResponse>> StartRegistration(
        string teamSlug,
        string eventSlug,
        StartRegistrationRequest request,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();

        // var teamId = await context.Teams.GetTeamIdAsync(teamSlug, cancellationToken);
        // var ticketedEvent = await context.TicketedEvents.GetTicketedEventAsync(
        //     teamId,
        //     eventSlug,
        //     true,
        //     cancellationToken);
        //
        // // Early exit: If there's not enough capacity, reject immediately.
        // if (request.Type == RegistrationType.Public && !ticketedEvent.HasAvailableCapacity(request.Tickets))
        // {
        //     throw ValidationError.TicketedEvent.SoldOut();
        // }
        //
        // var registration = Registration.Create(
        //     ticketedEvent.Id,
        //     request.Type,
        //     request.Email,
        //     request.FirstName,
        //     request.LastName,
        //     request.AdditionalDetails,
        //     request.Tickets);
        //
        // context.Registrations.Add(registration);
        //
        // // TODO Create alternative flow for users that are already registered.
        //
        // return TypedResults.Accepted(
        //     $"/teams/{teamSlug}/events/{eventSlug}/registrations/{registration.Id}",
        //     new StartRegistrationResponse(registration.Id));
    }
}