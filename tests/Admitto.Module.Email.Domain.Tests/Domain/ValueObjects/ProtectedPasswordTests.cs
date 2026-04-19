using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.ValueObjects;

[TestClass]
public sealed class ProtectedPasswordTests
{
    [TestMethod]
    public void FromCiphertext_RoundTripsValue()
    {
        var pwd = ProtectedPassword.FromCiphertext("ENCRYPTED:abc");

        pwd.Ciphertext.ShouldBe("ENCRYPTED:abc");
    }

    [TestMethod]
    public void ToString_DoesNotLeakCiphertext()
    {
        var pwd = ProtectedPassword.FromCiphertext("ENCRYPTED:abc");

        pwd.ToString().ShouldNotContain("ENCRYPTED");
    }
}
