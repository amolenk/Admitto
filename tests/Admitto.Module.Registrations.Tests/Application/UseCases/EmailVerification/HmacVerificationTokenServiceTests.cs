using Amolenk.Admitto.Module.Registrations.Application.Security;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.EmailVerification;

[TestClass]
public sealed class HmacVerificationTokenServiceTests
{
    // Base64 of 32 bytes: just for testing
    private const string TestSigningKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private static readonly EmailAddress DefaultEmail = EmailAddress.From("alice@example.com");
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly TeamId DefaultTeamId = TeamId.New();

    private static HmacVerificationTokenService CreateSut(FakeTimeProvider? fakeTime = null)
    {
        var options = Options.Create(new VerificationTokenOptions
        {
            SigningKey = TestSigningKey,
            TokenTtlMinutes = 15
        });
        return new HmacVerificationTokenService(options, fakeTime ?? new FakeTimeProvider());
    }

    [TestMethod]
    public void SC001_HmacTokenService_Issue_Validate_RoundTrip_ReturnsEmail()
    {
        var sut = CreateSut();

        var token = sut.Issue(DefaultEmail, DefaultEventId, DefaultTeamId);
        var claims = sut.Validate(token, DefaultEventId);

        claims.ShouldNotBeNull();
        claims.Email.ShouldBe(DefaultEmail);
    }

    [TestMethod]
    public void SC002_HmacTokenService_Validate_ExpiredToken_ReturnsNull()
    {
        var fakeTime = new FakeTimeProvider();
        var sut = CreateSut(fakeTime);

        var token = sut.Issue(DefaultEmail, DefaultEventId, DefaultTeamId);

        // Advance time past expiry
        fakeTime.Advance(TimeSpan.FromMinutes(16));

        var claims = sut.Validate(token, DefaultEventId);

        claims.ShouldBeNull();
    }

    [TestMethod]
    public void SC003_HmacTokenService_Validate_WrongEventId_ReturnsNull()
    {
        var sut = CreateSut();

        var token = sut.Issue(DefaultEmail, DefaultEventId, DefaultTeamId);

        var otherEventId = TicketedEventId.New();
        var claims = sut.Validate(token, otherEventId);

        claims.ShouldBeNull();
    }

    [TestMethod]
    public void SC004_HmacTokenService_Validate_TamperedToken_ReturnsNull()
    {
        var sut = CreateSut();

        var token = sut.Issue(DefaultEmail, DefaultEventId, DefaultTeamId);

        // Tamper with the signature part
        var parts = token.Split('.');
        parts[2] = parts[2][..^1] + (parts[2][^1] == 'A' ? 'B' : 'A');
        var tamperedToken = string.Join('.', parts);

        var claims = sut.Validate(tamperedToken, DefaultEventId);

        claims.ShouldBeNull();
    }

    [TestMethod]
    public void SC005_HmacTokenService_Validate_MalformedToken_ReturnsNull()
    {
        var sut = CreateSut();

        sut.Validate("not.a.valid.token.structure", DefaultEventId).ShouldBeNull();
        sut.Validate("only.two", DefaultEventId).ShouldBeNull();
        sut.Validate("", DefaultEventId).ShouldBeNull();
    }
}
