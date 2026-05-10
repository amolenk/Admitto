using Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmail;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmail.AdminApi;

public static class GetBulkEmailHttpEndpoint
{
    public static RouteGroupBuilder MapGetBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{bulkEmailJobId:guid}", GetBulkEmail)
            .WithName("GetBulkEmail")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<BulkEmailJobDetailDto>> GetBulkEmail(
        Guid teamId,
        Guid eventId,
        Guid bulkEmailJobId,
        GetBulkEmailHandler handler,
        CancellationToken ct)
    {
        var dto = await handler.HandleAsync(
            new GetBulkEmailQuery(BulkEmailJobId.From(bulkEmailJobId)), ct);

        if (dto is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Domain.Entities.BulkEmailJob>(bulkEmailJobId.ToString()));

        return TypedResults.Ok(dto);
    }
}

