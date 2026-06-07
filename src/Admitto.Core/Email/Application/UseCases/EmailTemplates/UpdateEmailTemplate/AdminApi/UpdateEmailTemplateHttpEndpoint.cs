using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

public static class UpdateEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "UpdateEventEmailTemplate" : "UpdateTeamEmailTemplate";

        group
            .MapPut("/{id:guid}", new Handler(isEventScoped).HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<Ok> HandleAsync(
            Guid id,
            Guid teamId,
            Guid? eventId,
            UpdateEmailTemplateHttpRequest request,
            ICommandHandler<UpdateEmailTemplateCommand> handler,
            [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var command = new UpdateEmailTemplateCommand(
                id,
                teamId,
                isEventScoped ? eventId!.Value : null,
                request.Name,
                request.Subject,
                request.TextBody,
                request.HtmlBody,
                request.Version);

            await handler.HandleAsync(command, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return TypedResults.Ok();
        }
    }
}
