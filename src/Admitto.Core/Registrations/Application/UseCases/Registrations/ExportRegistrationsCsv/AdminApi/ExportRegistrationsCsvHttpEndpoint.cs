using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ExportRegistrationsCsv.AdminApi;

public static class ExportRegistrationsCsvHttpEndpoint
{
    public static RouteGroupBuilder MapExportRegistrationsCsv(this RouteGroupBuilder group)
    {
        group
            .MapGet("/export", ExportRegistrationsCsv)
            .WithName(nameof(ExportRegistrationsCsv))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<FileContentHttpResult, NotFound>> ExportRegistrationsCsv(
        Guid teamId,
        Guid eventId,
        IQueryHandler<ExportRegistrationsCsvQuery, (string FileName, byte[] Content)?> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new ExportRegistrationsCsvQuery(eventId, teamId),
            cancellationToken);

        if (result is null)
            return TypedResults.NotFound();

        var (fileName, content) = result.Value;
        return TypedResults.File(content, "text/csv", fileName);
    }
}
