using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public static class TestSendEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapTestSendEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped
            ? "TestSendEventEmailTemplate"
            : "TestSendTeamEmailTemplate";

        group
            .MapPost("/{id:guid}/test-send", async (
                Guid id,
                Guid teamId,
                Guid? eventId,
                TestSendEmailTemplateHttpRequest request,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var command = isEventScoped
                    ? request.ToCommand(id, teamId, eventId!.Value)
                    : request.ToCommand(id, teamId, null);

                await mediator.SendAsync(command, ct);
                return TypedResults.Ok();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
