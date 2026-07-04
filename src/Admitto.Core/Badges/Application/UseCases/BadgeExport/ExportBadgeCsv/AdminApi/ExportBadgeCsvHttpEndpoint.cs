using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.BadgeExport.ExportBadgeCsv.AdminApi;

public static class ExportBadgeCsvHttpEndpoint
{
    public static RouteGroupBuilder MapExportBadgeCsv(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{badgeTypeId:guid}/export", ExportBadgeCsv)
            .WithName(nameof(ExportBadgeCsv))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<FileContentHttpResult> ExportBadgeCsv(
        Guid teamId,
        Guid eventId,
        Guid badgeTypeId,
        IQueryHandler<ExportBadgeCsvQuery, (string FileName, byte[] Content)> handler,
        CancellationToken cancellationToken)
    {
        var (fileName, content) = await handler.HandleAsync(
            new ExportBadgeCsvQuery(eventId, teamId, badgeTypeId),
            cancellationToken);

        return TypedResults.File(content, "text/csv", fileName);
    }
}
