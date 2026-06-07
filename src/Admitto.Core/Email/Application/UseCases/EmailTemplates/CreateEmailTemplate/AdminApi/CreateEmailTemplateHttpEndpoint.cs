using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;

public static class CreateEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapCreateEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "CreateEventEmailTemplate" : "CreateTeamEmailTemplate";

        var handler = new Handler(isEventScoped);

        group
            .MapPost("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<Created<CreateEmailTemplateResponse>> HandleAsync(
            Guid teamId,
            Guid? eventId,
            CreateEmailTemplateHttpRequest request,
            ICommandHandler<CreateEmailTemplateCommand, Guid> handler,
            [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var ticketedEventId = isEventScoped ? eventId!.Value : (Guid?)null;

            var command = new CreateEmailTemplateCommand(
                teamId,
                ticketedEventId,
                request.Name,
                request.Subject,
                request.TextBody,
                request.HtmlBody);

            var id = await handler.HandleAsync(command, ct);

            await unitOfWork.SaveChangesAsync(ct);

            var location = eventId is not null
                ? $"/admin/teams/{teamId}/events/{eventId}/email-templates/{id}"
                : $"/admin/teams/{teamId}/email-templates/{id}";

            return TypedResults.Created(location, new CreateEmailTemplateResponse(id));
        }
    }
}
