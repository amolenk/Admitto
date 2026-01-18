using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.Messaging;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.UseCases.Attendees.RecordAttendance;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.CheckIn;

public static class CheckInEndpoint
{
    public static RouteGroupBuilder MapCheckIn(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{attendeeId:guid}/privileged-check-in", CheckIn)
            .WithName($"Privileged{nameof(CheckIn)}")
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<CheckInResponse>> CheckIn(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        ISlugResolver slugResolver,
        IApplicationContext context,
        ISigningService signingService,
        IMessageOutbox outbox,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);
        
        var admissionRecord = await context.ParticipationView
            .Where(a => a.TicketedEventId == eventId && a.AttendeeId == attendeeId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (admissionRecord is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        // Publish a message to record the attendance.        
        if (admissionRecord.AttendeeId is not null && admissionRecord.AttendeeId != Guid.Empty)
        {
            outbox.Enqueue(new RecordAttendanceCommand(admissionRecord.AttendeeId.Value, Attended: true));
        }

        var response = new CheckInResponse(
            admissionRecord.Email,
            admissionRecord.FirstName,
            admissionRecord.LastName,
            admissionRecord.AttendeeStatus,
            admissionRecord.ContributorStatus,
            admissionRecord.LastModifiedAt);
        
        return TypedResults.Ok(response);
    }
}