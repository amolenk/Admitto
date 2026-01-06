using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.GetAttendee;

public static class GetAttendeeEndpoint
{
    public static RouteGroupBuilder MapGetAttendee(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{attendeeId:guid}", GetAttendee)
            .WithName(nameof(GetAttendee))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<GetAttendeeResponse>> GetAttendee(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var attendee = await context.Attendees
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == attendeeId, cancellationToken);
        if (attendee is null || attendee.TicketedEventId != eventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Attendee.NotFound);
        }

        var activities = await context.ParticipantActivityView
            .AsNoTracking()
            .Where(p => p.TicketedEventId == eventId && p.ParticipantId == attendee.ParticipantId)
            .GroupJoin(
                context.EmailLog,
                p => p.EmailLogId,
                l => l.Id,
                (p, ls) => new { Activity = p, Emails = ls })
            .SelectMany(
                x => x.Emails.DefaultIfEmpty(),
                (x, l) => new { x.Activity, Email = l })
            .Select(x => new ActivityDto(
                x.Activity.OccuredOn,
                x.Activity.Activity.ToString(),
                x.Email != null ? x.Email.EmailType : null))
            .ToArrayAsync(cancellationToken);

        var response = new GetAttendeeResponse(
            attendee.Id,
            attendee.Email,
            attendee.FirstName,
            attendee.LastName,
            attendee.RegistrationStatus,
            attendee.AdditionalDetails
                .Select(ad => new AdditionalDetailDto(ad.Name, ad.Value))
                .ToArray(),
            attendee.Tickets
                .Select(at => new TicketSelectionDto(at.TicketTypeSlug, at.Quantity))
                .ToArray(),
            activities,
            attendee.LastChangedAt);

        return TypedResults.Ok(response);
    }
}