using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class OtpCodeTests
{
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");

    [TestMethod]
    public void SC001_OtpCode_Create_RaisesOtpCodeRequestedDomainEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(10);

        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Test Event", DefaultEmail, "123456", expiresAt);

        var evt = sut.GetDomainEvents().OfType<OtpCodeRequestedDomainEvent>().ShouldHaveSingleItem();
        evt.TeamId.ShouldBe(DefaultTeamId);
        evt.TicketedEventId.ShouldBe(DefaultEventId);
        evt.EventName.ShouldBe("Test Event");
        evt.RecipientEmail.ShouldBe(DefaultEmail);
        evt.PlainCode.ShouldBe("123456");
    }

    [TestMethod]
    public void SC002_OtpCode_Create_HashesEmailAndCode()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Test Event", DefaultEmail, "123456",
            DateTimeOffset.UtcNow.AddMinutes(10));

        sut.EmailHash.ShouldNotBe("test@example.com");
        sut.CodeHash.ShouldNotBe("123456");
        sut.EmailHash.ShouldBe(OtpCode.ComputeEmailHash("test@example.com"));
    }

    [TestMethod]
    public void SC003_OtpCode_IsExpired_FalseBeforeExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000",
            now.AddMinutes(10));

        sut.IsExpired(now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC004_OtpCode_IsExpired_TrueAtOrAfterExpiry()
    {
        var expiresAt = DateTimeOffset.UtcNow;
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000", expiresAt);

        sut.IsExpired(expiresAt).ShouldBeTrue();
        sut.IsExpired(expiresAt.AddSeconds(1)).ShouldBeTrue();
    }

    [TestMethod]
    public void SC005_OtpCode_IsUsed_FalseInitially()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        sut.IsUsed.ShouldBeFalse();
    }

    [TestMethod]
    public void SC006_OtpCode_MarkUsed_SetsUsedAtAndIsUsed()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));
        var now = DateTimeOffset.UtcNow;

        sut.MarkUsed(now);

        sut.IsUsed.ShouldBeTrue();
        sut.UsedAt.ShouldBe(now);
    }

    [TestMethod]
    public void SC007_OtpCode_IsLocked_FalseBeforeFiveAttempts()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        for (var i = 0; i < 4; i++)
            sut.IncrementFailedAttempts();

        sut.IsLocked.ShouldBeFalse();
        sut.FailedAttempts.ShouldBe(4);
    }

    [TestMethod]
    public void SC008_OtpCode_IsLocked_TrueAfterFiveAttempts()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        for (var i = 0; i < 5; i++)
            sut.IncrementFailedAttempts();

        sut.IsLocked.ShouldBeTrue();
    }

    [TestMethod]
    public void SC009_OtpCode_IsSuperseded_FalseInitially()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        sut.IsSuperseded.ShouldBeFalse();
    }

    [TestMethod]
    public void SC010_OtpCode_Supersede_SetsSupersededAt()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, "Event", DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));
        var now = DateTimeOffset.UtcNow;

        sut.Supersede(now);

        sut.IsSuperseded.ShouldBeTrue();
        sut.SupersededAt.ShouldBe(now);
    }
}
