using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public static class TestSendEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapTestSendEmailTemplate(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped
            ? "TestSendEventEmailTemplate"
            : "TestSendTeamEmailTemplate";

        var handler = new Handler(isEventScoped);

        group
            .MapPost("/{id:guid}/test-send", handler.HandleAsync)
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
            TestSendEmailTemplateHttpRequest request,
            TestSendEmailTemplateHandler handler,
            CancellationToken ct)
        {
            var command = isEventScoped
                ? request.ToCommand(id, teamId, eventId!.Value)
                : request.ToCommand(id, teamId, null);

            await handler.HandleAsync(command, ct);
            return TypedResults.Ok();
        }
    }
}
