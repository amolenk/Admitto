using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;

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
