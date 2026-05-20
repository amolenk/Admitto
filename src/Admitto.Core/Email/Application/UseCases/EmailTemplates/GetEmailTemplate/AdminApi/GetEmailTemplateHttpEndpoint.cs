using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;

public static class GetEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "GetTeamEmailTemplate" : "GetEventEmailTemplate";
        var handler = new Handler();

        group
            .MapGet("/{id:guid}", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler()
    {
        public async ValueTask<Ok<EmailTemplateDto>> HandleAsync(
            Guid id,
            GetEmailTemplateHandler handler,
            CancellationToken ct)
        {
            var dto = await handler.HandleAsync(
                new GetEmailTemplateQuery(EmailTemplateId.From(id)), ct);

            if (dto is null)
                throw new BusinessRuleViolationException(
                    NotFoundError.Create<Domain.Entities.EmailTemplate>());

            return TypedResults.Ok(dto);
        }
    }
}
