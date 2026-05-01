using Amolenk.Admitto.Module.Registrations.Application.Common.Cryptography;
using Amolenk.Admitto.Module.Shared.Application.Cryptography;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.Common.Cryptography;

[TestClass]
public sealed class RegistrationSignerTests
{
    private static readonly TicketedEventId EventA = TicketedEventId.New();
    private static readonly TicketedEventId EventB = TicketedEventId.New();

    private static readonly byte[] KeyA = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
    private static readonly byte[] KeyB = Enumerable.Range(0, 32).Select(i => (byte)(i + 100)).ToArray();

    [TestMethod]
    public async Task SignAsync_SameIdDifferentEvents_ProduceDifferentSignatures()
    {
        var registrationId = Guid.NewGuid();
        var sut = NewSigner();

        var signedForA = await sut.SignAsync(registrationId, EventA);
        var signedForB = await sut.SignAsync(registrationId, EventB);

        signedForA.ShouldNotBe(signedForB);
    }

    [TestMethod]
    public async Task SignAsync_SameEventDifferentIds_ProduceDifferentSignatures()
    {
        var sut = NewSigner();

        var signedFirst = await sut.SignAsync(Guid.NewGuid(), EventA);
        var signedSecond = await sut.SignAsync(Guid.NewGuid(), EventA);

        signedFirst.ShouldNotBe(signedSecond);
    }

    [TestMethod]
    public async Task IsValidAsync_RoundTrip_ReturnsTrue()
    {
        var registrationId = Guid.NewGuid();
        var sut = NewSigner();

        var signature = await sut.SignAsync(registrationId, EventA);

        (await sut.IsValidAsync(registrationId, signature, EventA)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task IsValidAsync_SignatureFromOtherEvent_ReturnsFalse()
    {
        var registrationId = Guid.NewGuid();
        var sut = NewSigner();

        var signedForA = await sut.SignAsync(registrationId, EventA);

        (await sut.IsValidAsync(registrationId, signedForA, EventB)).ShouldBeFalse();
    }

    private static RegistrationSigner NewSigner()
    {
        var keyProvider = new StubKeyProvider(new Dictionary<TicketedEventId, byte[]>
        {
            [EventA] = KeyA,
            [EventB] = KeyB,
        });
        return new RegistrationSigner(keyProvider, new SigningService());
    }

    private sealed class StubKeyProvider(IReadOnlyDictionary<TicketedEventId, byte[]> keys) : IEventSigningKeyProvider
    {
        public ValueTask<ReadOnlyMemory<byte>> GetKeyAsync(
            TicketedEventId eventId,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<ReadOnlyMemory<byte>>(keys[eventId]);
    }
}
