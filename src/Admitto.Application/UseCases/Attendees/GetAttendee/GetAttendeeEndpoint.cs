using Amolenk.Admitto.Application.Common.Authorization;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendee;

public static class GetAttendeeEndpoint
{
    public static RouteGroupBuilder MapGetAttendee(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{attendeeId:guid}", GetAttendee)
            .WithName(nameof(GetAttendee))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetAttendeeResponse>> GetAttendee(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var attendee = await context.Attendees.GetEntityAsync(
            attendeeId,
            cancellationToken: cancellationToken);

        var response = new GetAttendeeResponse(attendee.Email, attendee.Status);
        
        return TypedResults.Ok(response);
    }
}