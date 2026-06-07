using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.GetEmailSettings.AdminApi;

public static class GetEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailSettings(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "GetEventEmailSettings" : "GetTeamEmailSettings";
        var handler = new Handler(isEventScoped);

        group
            .MapGet("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<Ok<EmailSettingsDto>> HandleAsync(
            Guid teamId,
            Guid? eventId,
            IQueryHandler<GetEmailSettingsQuery, EmailSettingsDto?> handler,
            CancellationToken ct)
        {
            var ticketedEventId = isEventScoped ? eventId!.Value : (Guid?)null;

            var dto = await handler.HandleAsync(
                new GetEmailSettingsQuery(teamId, ticketedEventId), ct);

            if (dto is null)
                throw new BusinessRuleViolationException(
                    NotFoundError.Create<Domain.Entities.EmailSettings>());

            return TypedResults.Ok(dto);
        }
    }
}
