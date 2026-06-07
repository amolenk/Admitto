using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate.AdminApi;

public static class GetEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "GetEventEmailTemplate" : "GetTeamEmailTemplate";
        var handler = new Handler(isEventScoped);

        group
            .MapGet("/{id:guid}", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<Ok<EmailTemplateDto>> HandleAsync(
            Guid id,
            Guid teamId,
            Guid? eventId,
            IQueryHandler<GetEmailTemplateQuery, EmailTemplateDto?> handler,
            CancellationToken ct)
        {
            var ticketedEventId = isEventScoped ? TicketedEventId.From(eventId!.Value) : (TicketedEventId?)null;
            var dto = await handler.HandleAsync(
                new GetEmailTemplateQuery(EmailTemplateId.From(id), TeamId.From(teamId), ticketedEventId), ct);

            if (dto is null)
                throw new BusinessRuleViolationException(
                    NotFoundError.Create<Domain.Entities.EmailTemplate>());

            return TypedResults.Ok(dto);
        }
    }
}
