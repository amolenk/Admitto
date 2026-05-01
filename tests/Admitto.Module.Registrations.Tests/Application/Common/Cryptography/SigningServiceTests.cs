using System.Text;
using Amolenk.Admitto.Module.Shared.Application.Cryptography;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.Common.Cryptography;

[TestClass]
public sealed class SigningServiceTests
{
    private static readonly byte[] KeyA = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
    private static readonly byte[] KeyB = Enumerable.Range(0, 32).Select(i => (byte)(255 - i)).ToArray();
    private static readonly byte[] Payload = Encoding.ASCII.GetBytes("payload-under-test");

    private static ISigningService NewSut() => new SigningService();

    [TestMethod]
    public void Sign_SamePayloadAndKey_IsDeterministic()
    {
        var sut = NewSut();

        var first = sut.Sign(Payload, KeyA);
        var second = sut.Sign(Payload, KeyA);

        first.ShouldBe(second);
        first.ShouldNotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void Sign_DifferentKeys_ProduceDifferentSignatures()
    {
        var sut = NewSut();

        var withKeyA = sut.Sign(Payload, KeyA);
        var withKeyB = sut.Sign(Payload, KeyB);

        withKeyA.ShouldNotBe(withKeyB);
    }

    [TestMethod]
    public void Sign_OutputIsUrlSafeBase64()
    {
        var sut = NewSut();

        var signature = sut.Sign(Payload, KeyA);

        signature.ShouldNotContain("+");
        signature.ShouldNotContain("/");
        signature.ShouldNotContain("=");
    }

    [TestMethod]
    public void IsValid_RoundTrip_ReturnsTrue()
    {
        var sut = NewSut();

        var signature = sut.Sign(Payload, KeyA);

        sut.IsValid(Payload, signature, KeyA).ShouldBeTrue();
    }

    [TestMethod]
    public void IsValid_TamperedSignature_ReturnsFalse()
    {
        var sut = NewSut();

        var signature = sut.Sign(Payload, KeyA);
        var tampered = signature[..^1] + (signature[^1] == 'A' ? 'B' : 'A');

        sut.IsValid(Payload, tampered, KeyA).ShouldBeFalse();
    }

    [TestMethod]
    public void IsValid_EmptySignature_ReturnsFalse()
    {
        var sut = NewSut();

        sut.IsValid(Payload, string.Empty, KeyA).ShouldBeFalse();
    }

    [TestMethod]
    public void IsValid_WrongKey_ReturnsFalse()
    {
        var sut = NewSut();

        var signature = sut.Sign(Payload, KeyA);

        sut.IsValid(Payload, signature, KeyB).ShouldBeFalse();
    }

}
