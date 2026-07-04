using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetQRCode.PublicApi;

public static class GetQRCodeHttpEndpoint
{
    public static RouteGroupBuilder MapGetQRCode(this RouteGroupBuilder group)
    {
        group.MapGet("/{eventSlug}/qr-code/{registrationId:guid}", GetQRCode)
            .WithName(nameof(GetQRCode))
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static async ValueTask<FileContentHttpResult> GetQRCode(
        string eventSlug,
        Guid registrationId,
        IQueryHandler<GetQRCodeQuery, GetQRCodeResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetQRCodeQuery(eventSlug, registrationId), cancellationToken);
        return TypedResults.File(result.Content, result.ContentType, result.FileName);
    }
}
