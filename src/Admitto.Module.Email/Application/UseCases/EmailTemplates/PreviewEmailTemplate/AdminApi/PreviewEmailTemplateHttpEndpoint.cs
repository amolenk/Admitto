using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;

public static class PreviewEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapPreviewEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "PreviewEventEmailTemplate" : "PreviewTeamEmailTemplate";

        group
            .MapPost("/preview", async (
                Guid teamId,
                Guid? eventId,
                PreviewEmailTemplateHttpRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var query = new PreviewEmailTemplateQuery(
                    request.Subject,
                    request.TextBody,
                    request.HtmlBody);

                var dto = await mediator.QueryAsync<PreviewEmailTemplateQuery, PreviewEmailTemplateDto>(query, ct);
                return TypedResults.Ok(dto);
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
