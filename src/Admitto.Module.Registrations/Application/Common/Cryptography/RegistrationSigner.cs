using System.Text;
using Amolenk.Admitto.Module.Shared.Application.Cryptography;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Application.Common.Cryptography;

/// <summary>
/// Composes <see cref="IEventSigningKeyProvider"/> and <see cref="ISigningService"/>
/// to sign and verify registration-bound URLs (QR codes, future signed links).
/// The signed payload is the registration id's lowercase hex form, scoped per event
/// by virtue of the per-event signing key.
/// </summary>
public sealed class RegistrationSigner(
    IEventSigningKeyProvider keyProvider,
    ISigningService signingService)
{
    public async ValueTask<string> SignAsync(
        Guid registrationId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        var key = await keyProvider.GetKeyAsync(eventId, cancellationToken);
        var payload = Encoding.ASCII.GetBytes(registrationId.ToString("N"));

        return signingService.Sign(payload, key.Span);
    }

    public async ValueTask<bool> IsValidAsync(
        Guid registrationId,
        string signature,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        var key = await keyProvider.GetKeyAsync(eventId, cancellationToken);
        var payload = Encoding.ASCII.GetBytes(registrationId.ToString("N"));

        return signingService.IsValid(payload, signature, key.Span);
    }
}
