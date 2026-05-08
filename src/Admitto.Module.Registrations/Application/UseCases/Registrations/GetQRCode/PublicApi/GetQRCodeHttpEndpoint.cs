using Amolenk.Admitto.Module.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetQRCode.PublicApi;

public static class GetQRCodeHttpEndpoint
{
    public static RouteGroupBuilder MapGetQRCode(this RouteGroupBuilder group)
    {
        group.MapGet("/registrations/{registrationId:guid}/qr-code", HandleAsync)
            .WithName(nameof(GetQRCodeHttpEndpoint));

        return group;
    }

    private static async ValueTask<FileContentHttpResult> HandleAsync(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        string? signature,
        RegistrationSigner registrationSigner,
        IRegistrationsWriteStore writeStore,
        CancellationToken cancellationToken)
    {
        var typedEventId = TicketedEventId.From(eventId);

        if (string.IsNullOrEmpty(signature) ||
            !await registrationSigner.IsValidAsync(registrationId, signature, typedEventId, cancellationToken))
        {
            throw new BusinessRuleViolationException(Errors.InvalidSignature);
        }

        var typedRegistrationId = RegistrationId.From(registrationId);

        var registrationExists = await writeStore.Registrations
            .AsNoTracking()
            .AnyAsync(
                r => r.Id == typedRegistrationId && r.EventId == typedEventId,
                cancellationToken);

        if (!registrationExists)
            throw new BusinessRuleViolationException(Errors.RegistrationNotFound);

        var qrCodeBytes = GenerateQRCode($"{registrationId}:{signature}");

        return TypedResults.File(qrCodeBytes, "image/png", "qrcode.png");
    }

    private static byte[] GenerateQRCode(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    internal static class Errors
    {
        public static readonly Error InvalidSignature = new(
            "registration.invalid_signature",
            "The provided signature is missing or invalid.",
            Type: ErrorType.Forbidden);

        public static readonly Error RegistrationNotFound = new(
            "registration.not_found",
            "The registration could not be found.",
            Type: ErrorType.NotFound);
    }
}
