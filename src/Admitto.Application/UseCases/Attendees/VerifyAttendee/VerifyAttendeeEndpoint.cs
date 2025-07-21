using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.Attendees.VerifyAttendee;

public static class VerifyAttendeeEndpoint
{
    public static RouteGroupBuilder MapVerifyAttendee(this RouteGroupBuilder group)
    {
        group
            .MapPut("/{attendeeId:guid}/verify", VerifyAttendee)
            .WithName(nameof(VerifyAttendee))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> VerifyAttendee(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        VerifyAttendeeRequest request,
        IDomainContext context,
        CancellationToken cancellationToken)
    {
        var attendee = await context.Attendees.GetEntityAsync(
            attendeeId,
            cancellationToken: cancellationToken);

        var result = attendee.Verify(request.Code);

        // TODO Return result in a way that the client can understand.
        
        return TypedResults.Ok();
    }
}