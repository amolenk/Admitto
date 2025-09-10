using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Application.Common.Identity;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Public.Register;

/// <summary>
/// Represents the endpoint for creating a new registration for a ticketed event.
/// </summary>
public static class RegisterEndpoint
{
    public static RouteGroupBuilder MapRegister(this RouteGroupBuilder group)
    {
        group
            .MapPost("/register", Register)
            .WithName(nameof(Register));

        return group;
    }

    private static async ValueTask<Created> Register(
        string teamSlug,
        string eventSlug,
        RegisterRequest request,
        ISigningService signingService,
        ISlugResolver slugResolver,
        IApplicationContext context,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        await EnsureValidRequestAsync(request, eventId, signingService, cancellationToken);

        // First get or create a participant. A participant may already exist if the same person is also
        // a contributor to the event.
        var (participant, created) = await GetOrCreateParticipantAsync(
            teamId,
            eventId,
            request.Email,
            context,
            unitOfWork,
            cancellationToken);

        // If the participant already exists, ensure they are not already registered.
        if (!created)
        {
            await EnsureParticipantNotRegisteredAsync(participant, context, cancellationToken);
        }

        // Claim the tickets. If the registration is done by an organizer on behalf of the actual attendee,
        // ignore the ticket limits.
        await ClaimTicketsAsync(
            eventId,
            participant,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.Tickets.Distinct().Select(t => new TicketSelection(t, 1)).ToList(),
            ignoreCapacity: false,
            context,
            cancellationToken);

        return TypedResults.Created();
    }

    private static async ValueTask EnsureValidRequestAsync(
        RegisterRequest request,
        Guid eventId,
        ISigningService signingService,
        CancellationToken cancellationToken)
    {
        var email = request.Email.NormalizeEmail();

        // Ensure the email has been verified using the provided token.
        if (request.VerificationToken is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.InvalidVerificationToken);
        }

        var token = await EmailVerifiedToken.TryDecodeAndValidateAsync(
            request.VerificationToken,
            eventId,
            signingService,
            cancellationToken);

        if (token?.Email != email)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.InvalidVerificationToken);
        }
    }

    private static async ValueTask<(Participant Participant, bool Created)> GetOrCreateParticipantAsync(
        Guid teamId,
        Guid eventId,
        string email,
        IApplicationContext context,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var emailNormalized = email.NormalizeEmail();

        var participant = await context.Participants.SingleOrDefaultAsync(
            p => p.TicketedEventId == eventId && p.Email == emailNormalized,
            cancellationToken);

        if (participant is not null) return (participant, false);

        participant = Participant.Create(teamId, eventId, emailNormalized);
        var created = true;

        context.Participants.Add(participant);

        await unitOfWork.SaveChangesAsync(
            async () =>
            {
                // Another thread created it first: fetch the existing one.
                participant = await context.Participants.SingleAsync(
                    p => p.TicketedEventId == eventId && p.Email == emailNormalized,
                    cancellationToken);

                created = false;
            },
            cancellationToken);

        return (participant, created);
    }

    // We can remove this if we let the domain event run in the same transaction.
    private static async ValueTask EnsureParticipantNotRegisteredAsync(
        Participant participant,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var attendeeExists = await context.Attendees
            .Where(a => a.ParticipantId == participant.Id)
            .AnyAsync(cancellationToken);

        if (attendeeExists)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.AlreadyRegistered);
        }
    }

    private static async ValueTask ClaimTicketsAsync(
        Guid eventId,
        Participant participant,
        string firstName,
        string lastName,
        IList<AdditionalDetail> additionalDetails,
        IList<TicketSelection> tickets,
        bool ignoreCapacity,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var availability = await context.TicketedEventAvailability.SingleOrDefaultAsync(
            tea => tea.TicketedEventId == eventId,
            cancellationToken);

        if (availability is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        availability.ClaimTickets(
            eventId,
            participant.Id,
            participant.Email,
            firstName,
            lastName,
            additionalDetails,
            tickets,
            ignoreCapacity);
    }
}