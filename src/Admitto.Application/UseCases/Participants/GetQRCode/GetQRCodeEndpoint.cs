using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Cryptography;
using QRCoder;

namespace Amolenk.Admitto.Application.UseCases.Participants.GetQRCode;

public static class GetQRCodeEndpoint
{
    public static RouteGroupBuilder MapGetQRCode(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{publicId:guid}/qr-code", GetQRCode)
            .WithName(nameof(GetQRCode))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<FileContentHttpResult> GetQRCode(
        string teamSlug,
        string eventSlug,
        Guid publicId,
        string signature,
        ISlugResolver slugResolver,
        ISigningService signingService,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);
        
        if (!await signingService.IsValidAsync(publicId, signature, eventId, cancellationToken))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Signing.InvalidSignature);
        }

        var qrCodeBytes = GenerateQRCode($"{publicId}:{signature}");

        return TypedResults.File(qrCodeBytes, "image/png", "qrcode.png");
    }
    
    private static byte[] GenerateQRCode(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }
}