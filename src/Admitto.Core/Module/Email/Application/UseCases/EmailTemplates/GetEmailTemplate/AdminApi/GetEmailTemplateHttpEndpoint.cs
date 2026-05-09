using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;

public static class GetEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "GetTeamEmailTemplate" : "GetEventEmailTemplate";

        group
            .MapGet("/{id:guid}", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.QueryAsync<GetEmailTemplateQuery, EmailTemplateDto?>(
                    new GetEmailTemplateQuery(EmailTemplateId.From(id)), ct);

                if (dto is null)
                    throw new BusinessRuleViolationException(
                        NotFoundError.Create<Domain.Entities.EmailTemplate>(id.ToString()));

                return TypedResults.Ok(dto);
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
