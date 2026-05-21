using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;

public static class PreviewEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapPreviewEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "PreviewEventEmailTemplate" : "PreviewTeamEmailTemplate";

        group
            .MapPost("/preview", PreviewEmailTemplate)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<PreviewEmailTemplateDto>> PreviewEmailTemplate(
        Guid teamId,
        Guid? eventId,
        PreviewEmailTemplateHttpRequest request,
        IQueryHandler<PreviewEmailTemplateQuery, PreviewEmailTemplateDto> handler,
        CancellationToken ct)
    {
        var query = new PreviewEmailTemplateQuery(
            request.Subject,
            request.TextBody,
            request.HtmlBody);

        var dto = await handler.HandleAsync(query, ct);
        return TypedResults.Ok(dto);
    }
}
