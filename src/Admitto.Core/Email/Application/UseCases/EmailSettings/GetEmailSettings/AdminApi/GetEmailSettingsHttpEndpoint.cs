using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;

public static class GetEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailSettings(
        this RouteGroupBuilder group)
    {
        group
            .MapGet("/", HandleAsync)
            .WithName("GetTeamEmailSettings")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<EmailSettingsDto>> HandleAsync(
        Guid teamId,
        IQueryHandler<GetEmailSettingsQuery, EmailSettingsDto?> handler,
        CancellationToken ct)
    {
        var dto = await handler.HandleAsync(new GetEmailSettingsQuery(teamId), ct);

        if (dto is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Domain.Entities.EmailSettings>());

        return TypedResults.Ok(dto);
    }
}
