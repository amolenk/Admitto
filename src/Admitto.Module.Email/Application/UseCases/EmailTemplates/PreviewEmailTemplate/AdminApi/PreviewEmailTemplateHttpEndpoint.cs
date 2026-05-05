using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.PreviewEmailTemplate.AdminApi;

public static class PreviewEmailTemplateHttpEndpoint
{
    private static readonly HashSet<string> KnownTypes =
    [
        EmailTemplateType.Ticket,
        EmailTemplateType.Reconfirm,
        EmailTemplateType.Cancellation,
        EmailTemplateType.VisaLetterDenied,
        EmailTemplateType.OtpCode,
    ];

    private static readonly Error UnknownTemplateType = new(
        "email_template.unknown_type",
        "The specified template type is not supported.");

    public static RouteGroupBuilder MapPreviewEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "PreviewEventEmailTemplate" : "PreviewTeamEmailTemplate";

        group
            .MapGet("/preview", async (
                string teamSlug,
                string? eventSlug,
                string type,
                IOrganizationScopeResolver scopeResolver,
                IMediator mediator,
                CancellationToken ct) =>
            {
                if (!KnownTypes.Contains(type))
                    throw new BusinessRuleViolationException(UnknownTemplateType);

                var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);

                var query = isEventScoped
                    ? new PreviewEmailTemplateQuery(
                        TeamId.From(orgScope.TeamId),
                        TicketedEventId.From(orgScope.EventId!.Value),
                        type)
                    : new PreviewEmailTemplateQuery(
                        TeamId.From(orgScope.TeamId),
                        null,
                        type);

                var dto = await mediator.QueryAsync<PreviewEmailTemplateQuery, PreviewEmailTemplateDto>(query, ct);
                return TypedResults.Ok(dto);
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        var draftEndpointName = isEventScoped ? "PreviewDraftEventEmailTemplate" : "PreviewDraftTeamEmailTemplate";

        group
            .MapPost("/preview", async (
                string teamSlug,
                string? eventSlug,
                string type,
                PreviewDraftEmailTemplateHttpRequest request,
                IOrganizationScopeResolver scopeResolver,
                IEmailRenderer renderer,
                CancellationToken ct) =>
            {
                if (!KnownTypes.Contains(type))
                    throw new BusinessRuleViolationException(UnknownTemplateType);

                // Resolve scope to verify team access and authorization.
                _ = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);

                var draftTemplate = EmailTemplate.Create(
                    EmailSettingsScope.Team,
                    Guid.NewGuid(),
                    type,
                    request.Subject ?? string.Empty,
                    request.TextBody ?? string.Empty,
                    request.HtmlBody ?? string.Empty);

                var parameters = EmailTemplateSampleParameters.Create();

                RenderedEmail rendered;
                try
                {
                    rendered = renderer.Render(draftTemplate, parameters);
                }
                catch (EmailRenderException ex)
                {
                    throw new BusinessRuleViolationException(new Error("email_template.render_failed", ex.Message));
                }

                return TypedResults.Ok(new PreviewEmailTemplateDto(
                    rendered.Subject,
                    rendered.TextBody,
                    rendered.HtmlBody));
            })
            .WithName(draftEndpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
