using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// Registers an attendee for a ticketed event.
/// </summary>
public class RegisterAttendeeHandler(IApplicationContext context, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterAttendeeCommand>
{
    public async ValueTask HandleAsync(RegisterAttendeeCommand command, CancellationToken cancellationToken)
    {
        // First get or create a participant. A participant may already exist if the same person is also
        // a contributor to the event.
        // We also use the participation view to check if the participant is already registered, so we can
        // fail fast in that case.
        var participantId = await GetOrCreateParticipant(command, cancellationToken);

        // Get the ticketed event.
        var ticketedEvent = await context.TicketedEvents.FindAsync([command.TicketedEventId], cancellationToken);
        if (ticketedEvent is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.TicketedEvent.NotFound);
        }

        // Claim the tickets. This can fail if there are not enough tickets available.
        ticketedEvent.ClaimTickets(
            command.Email,
            DateTimeOffset.UtcNow,
            command.RequestedTickets,
            ignoreCapacity: command.AdminOnBehalfOf);
        
        // There might be an existing registration for this participant.
        var existingAttendee = await context.Attendees
            .FirstOrDefaultAsync(a =>
                    a.TicketedEventId == command.TicketedEventId &&
                    a.ParticipantId == participantId,
                cancellationToken);

        // If there is an existing registration and it is canceled, we can remove it.
        // Otherwise, we should fail the operation.
        if (existingAttendee is not null)
        {
            if (existingAttendee.RegistrationStatus == RegistrationStatus.Canceled)
            {
                context.Attendees.Remove(existingAttendee);
            }
            else
            {
                throw new ApplicationRuleException(ApplicationRuleError.Attendee.AlreadyRegistered);
            }
        }
        
        // Add the new attendee registration.
        context.Attendees.Add(
            Attendee.Create(
                command.TicketedEventId,
                participantId,
                command.Email,
                command.FirstName,
                command.LastName,
                command.AdditionalDetails,
                command.RequestedTickets,
                ticketedEvent.AdditionalDetailSchemas));

        // Handle unique violations that may occur due to concurrent requests.
        unitOfWork.OnUniqueViolation = args =>
        {
            // If we fail because some other thread created the participant first, we should consider that an
            // optimistic concurrency failure and retry the entire operation.
            if (args.Error == ApplicationRuleError.Participant.AlreadyExists)
            {
                args.Retry = true;
            }
        };
    }

    private async ValueTask<Guid> GetOrCreateParticipant(
        RegisterAttendeeCommand command,
        CancellationToken cancellationToken)
    {
        // First check if the participant already exists. The attendee could already be a contributor to the event.
        var existingParticipant = await context.ParticipationView
            .Where(p => p.TicketedEventId == command.TicketedEventId && p.Email == command.Email)
            .Select(p => new
            {
                p.ParticipantId,
                p.AttendeeStatus
            })
            .FirstOrDefaultAsync(cancellationToken);

        Guid participantId;
        if (existingParticipant is not null)
        {
            // If the participant is already registered, we can stop here.
            if (existingParticipant.AttendeeStatus == ParticipationAttendeeStatus.Registered)
            {
                throw new ApplicationRuleException(ApplicationRuleError.Attendee.AlreadyRegistered);
            }

            participantId = existingParticipant.ParticipantId;
        }
        else
        {
            // Create a new participant.
            var newParticipant = Participant.Create(command.TeamId, command.TicketedEventId, command.Email);
            context.Participants.Add(newParticipant);

            participantId = newParticipant.Id;
        }

        return participantId;
    }
}