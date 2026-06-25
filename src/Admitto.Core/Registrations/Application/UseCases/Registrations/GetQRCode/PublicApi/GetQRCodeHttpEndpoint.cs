using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using QRCoder;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetQRCode.PublicApi;

public static class GetQRCodeHttpEndpoint
{
    public static RouteGroupBuilder MapGetQRCode(this RouteGroupBuilder group)
    {
        group.MapGet("/registrations/{registrationId:guid}/qr-code", GetQRCode)
            .WithName(nameof(GetQRCode));

        return group;
    }

    private static async ValueTask<FileContentHttpResult> GetQRCode(
        HttpContext httpContext,
        Guid eventId,
        Guid registrationId,
        IRegistrationsWriteStore writeStore,
        CancellationToken cancellationToken)
    {
        var teamId = httpContext.User.GetRequiredTeamId();
        var typedEventId = TicketedEventId.From(eventId);
        var typedTeamId = TeamId.From(teamId);

        var eventExists = await writeStore.TicketedEvents
            .AsNoTracking()
            .AnyAsync(e => e.Id == typedEventId && e.TeamId == typedTeamId, cancellationToken);

        if (!eventExists)
            throw new BusinessRuleViolationException(Errors.EventNotFound);

        var typedRegistrationId = RegistrationId.From(registrationId);

        var registrationExists = await writeStore.Registrations
            .AsNoTracking()
            .AnyAsync(
                r => r.Id == typedRegistrationId && r.EventId == typedEventId && r.TeamId == typedTeamId,
                cancellationToken);

        if (!registrationExists)
            throw new BusinessRuleViolationException(Errors.RegistrationNotFound);

        var qrCodeBytes = GenerateQRCode(registrationId.ToString());

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
