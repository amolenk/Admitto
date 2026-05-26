using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

public static class CreateBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapCreateBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateBulkEmail)
            .WithName("CreateBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Created<CreateBulkEmailResponse>> CreateBulkEmail(
        Guid teamId,
        Guid eventId,
        CreateBulkEmailHttpRequest request,
        ICommandHandler<CreateBulkEmailCommand, Guid> handler,
        [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken ct)
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

        var bulkEmailJobId = await handler.HandleAsync(command, ct);

        await unitOfWork.SaveChangesAsync(ct);

        var location = $"/admin/teams/{teamId}/events/{eventId}/bulk-emails/{bulkEmailJobId}";

        return TypedResults.Created(location, new CreateBulkEmailResponse(bulkEmailJobId));
    }
}
