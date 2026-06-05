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
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "DeleteTeamEmailTemplate" : "DeleteEventEmailTemplate";
        var handler = new Handler();

        group
            .MapDelete("/{id:guid}", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler()
    {
        public async ValueTask<NoContent> HandleAsync(
            Guid id,
            [FromQuery] uint version,
            ICommandHandler<DeleteEmailTemplateCommand> handler,
            [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            await handler.HandleAsync(new DeleteEmailTemplateCommand(id, version), ct);
            await unitOfWork.SaveChangesAsync(ct);

            return TypedResults.NoContent();
        }
    }
}
