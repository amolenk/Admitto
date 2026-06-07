using Amolenk.Admitto.Core.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Core.Shared.Application.Auth;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

public static class PreviewBulkEmailHttpEndpoint
{
    private const int SampleSize = 100;

    public static RouteGroupBuilder MapPreviewBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/preview", PreviewBulkEmail)
            .WithName("PreviewBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    // TODO Should be command/query
    private static async ValueTask<Ok<PreviewBulkEmailResponse>> PreviewBulkEmail(
        Guid teamId,
        Guid eventId,
        PreviewBulkEmailHttpRequest request,
        IBulkEmailRecipientResolver recipientResolver,
        CancellationToken ct)
    {
        var source = request.Source.ToDomain();
        var recipients = await recipientResolver.ResolveAsync(
            TeamId.From(teamId),
            TicketedEventId.From(eventId),
            source,
            ct);

        var sample = recipients
            .Take(SampleSize)
            .Select(r => new BulkEmailRecipientPreviewDto(r.Email.Value, r.DisplayName))
            .ToList();

        return TypedResults.Ok(new PreviewBulkEmailResponse(recipients.Count, sample));
    }
}
