using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate.AdminApi;

public static class DeleteEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "DeleteEventEmailTemplate" : "DeleteTeamEmailTemplate";
        var handler = new Handler(isEventScoped);

        group
            .MapDelete("/{id:guid}", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<NoContent> HandleAsync(
            Guid id,
            Guid teamId,
            Guid? eventId,
            [FromQuery] uint version,
            ICommandHandler<DeleteEmailTemplateCommand> handler,
            [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            await handler.HandleAsync(
                new DeleteEmailTemplateCommand(
                    id,
                    teamId,
                    isEventScoped ? eventId!.Value : null,
                    version), ct);
            await unitOfWork.SaveChangesAsync(ct);

            return TypedResults.NoContent();
        }
    }
}
