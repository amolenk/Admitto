using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

public static class CreateBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapCreateBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", async (
                string teamSlug,
                string eventSlug,
                CreateBulkEmailHttpRequest request,
                IOrganizationScopeResolver scopeResolver,
                IMediator mediator,
                [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);

                var command = new CreateBulkEmailCommand(
                    orgScope.TeamId,
                    orgScope.EventId!.Value,
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
                    $"/admin/teams/{teamSlug}/events/{eventSlug}/bulk-emails/{bulkEmailJobId}";

                return TypedResults.Created(location, new CreateBulkEmailResponse(bulkEmailJobId));
            })
            .WithName("CreateBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}

public sealed record CreateBulkEmailResponse(Guid BulkEmailJobId);
