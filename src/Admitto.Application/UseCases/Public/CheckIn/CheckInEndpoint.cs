using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.UseCases.Attendees.RecordAttendance;

namespace Amolenk.Admitto.Application.UseCases.Public.CheckIn;

public static class CheckInEndpoint
{
    public static RouteGroupBuilder MapCheckIn(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{publicId:guid}/check-in", CheckIn)
            .WithName(nameof(CheckIn))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<CheckInResponse>> CheckIn(
        string teamSlug,
        string eventSlug,
        Guid publicId,
        string signature,
        ISlugResolver slugResolver,
        IApplicationContext context,
        ISigningService signingService,
        IMessageOutbox outbox,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);
        
        if (!await signingService.IsValidAsync(publicId, signature, eventId, cancellationToken))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Signing.InvalidSignature);
        }

        var admissionRecord = await context.AdmissionView
            .Where(a => a.TicketedEventId == eventId && a.PublicId == publicId)
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