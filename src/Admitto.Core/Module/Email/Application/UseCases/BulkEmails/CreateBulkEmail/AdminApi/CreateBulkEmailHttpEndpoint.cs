using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

public static class CreateBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapCreateBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", async (
                Guid teamId,
                Guid eventId,
                CreateBulkEmailHttpRequest request,
                IMediator mediator,
                [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                var command = new CreateBulkEmailCommand(
                    teamId,
                    eventId,
                    request.EmailType,
                    request.TemplateName,
                    request.Subject,
                    request.TextBody,
                    request.HtmlBody,
                    request.Source.ToDomain());

                var bulkEmailJobId = await mediator
                    .SendReceiveAsync<CreateBulkEmailCommand, Guid>(command, ct);

                await unitOfWork.SaveChangesAsync(ct);

                var location =
                    $"/admin/teams/{teamId}/events/{eventId}/bulk-emails/{bulkEmailJobId}";

                return TypedResults.Created(location, new CreateBulkEmailResponse(bulkEmailJobId));
            })
            .WithName("CreateBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}

public sealed record CreateBulkEmailResponse(Guid BulkEmailJobId);
