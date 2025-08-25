using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Application.Common.QRCodes;

namespace Amolenk.Admitto.Application.UseCases.Registrations.GetQRCode;

public static class GetQRCodeEndpoint
{
    public static RouteGroupBuilder MapGetQRCode(this RouteGroupBuilder group)
    {
        group
            .MapGet("/{registrationId:guid}/qr-code", GetQRCode)
            .WithName(nameof(GetQRCode))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static FileContentHttpResult GetQRCode(
        Guid registrationId,
        string signature,
        ISigningService signingService,
        QRCodeGenerator qrCodeGenerator)
    {
        if (!signingService.IsValid(registrationId, signature))
        {
            throw new ApplicationRuleException(ApplicationRuleError.Registration.InvalidSignature);
        }

        var qrCodeBytes = qrCodeGenerator.GenerateRegistrationQRCode(registrationId);

        return TypedResults.File(qrCodeBytes, "image/png", "qrcode.png");
    }
}