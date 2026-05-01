using Amolenk.Admitto.Module.Email.Application.Sending.Bulk;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.PreviewBulkEmail.AdminApi;

public static class PreviewBulkEmailHttpEndpoint
{
    private const int SampleSize = 100;

    public static RouteGroupBuilder MapPreviewBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/preview", async (
                string teamSlug,
                string eventSlug,
                PreviewBulkEmailHttpRequest request,
                IOrganizationScopeResolver scopeResolver,
                IBulkEmailRecipientResolver recipientResolver,
                CancellationToken ct) =>
            {
                var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);

                var source = request.Source.ToDomain();
                var recipients = await recipientResolver.ResolveAsync(
                    TicketedEventId.From(orgScope.EventId!.Value),
                    source,
                    ct);

                var sample = recipients
                    .Take(SampleSize)
                    .Select(r => new BulkEmailRecipientPreviewDto(r.Email, r.DisplayName))
                    .ToList();

                return TypedResults.Ok(new PreviewBulkEmailResponse(recipients.Count, sample));
            })
            .WithName("PreviewBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
