using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;

public static class GetEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailSettings(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "GetTeamEmailSettings" : "GetEventEmailSettings";

        group
            .MapGet("/", async (
                Guid teamId,
                Guid? eventId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var scopeId = scope == EmailSettingsScope.Event ? eventId!.Value : teamId;

                var dto = await mediator.QueryAsync<GetEmailSettingsQuery, EmailSettingsDto?>(
                    new GetEmailSettingsQuery(scope, scopeId), ct);

                if (dto is null)
                    throw new BusinessRuleViolationException(
                        NotFoundError.Create<Domain.Entities.EmailSettings>(teamId.ToString()));

                return TypedResults.Ok(dto);
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
