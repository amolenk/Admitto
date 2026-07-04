using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using QRCoder;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetQRCode;

internal sealed class GetQRCodeHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetQRCodeQuery, GetQRCodeResult>
{
    public async ValueTask<GetQRCodeResult> HandleAsync(
        GetQRCodeQuery query,
        CancellationToken cancellationToken)
    {
        var eventSlug = Slug.From(query.EventSlug);
        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .Where(e => e.PublicSlug == eventSlug)
            .Select(e => new { e.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (ticketedEvent is null)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        var registrationId = RegistrationId.From(query.RegistrationId);
        var registrationExists = await writeStore.Registrations
            .AsNoTracking()
            .AnyAsync(
                r => r.Id == registrationId && r.EventId == ticketedEvent.Id,
                cancellationToken);

        if (!registrationExists)
            throw new BusinessRuleViolationException(Errors.RegistrationNotFound);

        return new GetQRCodeResult(
            GenerateQRCode(query.RegistrationId.ToString()),
            "image/png",
            "qrcode.png");
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
        public static readonly Error EventNotFound = new(
            "ticketed_event.not_found",
            "The ticketed event could not be found.",
            Type: ErrorType.NotFound);

        public static readonly Error RegistrationNotFound = new(
            "registration.not_found",
            "The registration could not be found.",
            Type: ErrorType.NotFound);
    }
}
