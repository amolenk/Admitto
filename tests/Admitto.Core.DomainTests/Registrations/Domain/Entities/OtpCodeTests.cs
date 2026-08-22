using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class OtpCodeTests
{
    private static readonly TeamId DefaultTeamId = TeamId.New();
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly EmailAddress DefaultEmail = EmailAddress.From("test@example.com");

    // When an OTP code is created
    // Then it raises a single OtpCodeRequested domain event carrying the team, event, recipient and plain code
    [TestMethod]
    public void OtpCode_Create_RaisesOtpCodeRequestedDomainEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(10);

        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Test Event"), DefaultEmail, "123456", expiresAt);

        var evt = sut.GetDomainEvents().OfType<OtpCodeRequestedDomainEvent>().ShouldHaveSingleItem();
        evt.TeamId.ShouldBe(DefaultTeamId);
        evt.TicketedEventId.ShouldBe(DefaultEventId);
        evt.EventName.ShouldBe(EventName.From("Test Event"));
        evt.RecipientEmail.ShouldBe(DefaultEmail);
        evt.PlainCode.ShouldBe("123456");
    }

    // When an OTP code is created with a plain email and code
    // Then the stored email and code hashes differ from the plain values and match the computed email hash
    [TestMethod]
    public void OtpCode_Create_HashesEmailAndCode()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Test Event"), DefaultEmail, "123456",
            DateTimeOffset.UtcNow.AddMinutes(10));

        sut.EmailHash.ShouldNotBe("test@example.com");
        sut.CodeHash.ShouldNotBe("123456");
        sut.EmailHash.ShouldBe(OtpCode.ComputeEmailHash("test@example.com"));
    }

    // Given an OTP code that expires 10 minutes from now
    // When checking expiry at the current time
    // Then it is not expired
    [TestMethod]
    public void OtpCode_IsExpired_FalseBeforeExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000",
            now.AddMinutes(10));

        sut.IsExpired(now).ShouldBeFalse();
    }

    // Given an OTP code with a known expiry time
    // When checking expiry at or after that time
    // Then it is expired
    [TestMethod]
    public void OtpCode_IsExpired_TrueAtOrAfterExpiry()
    {
        var expiresAt = DateTimeOffset.UtcNow;
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000", expiresAt);

        sut.IsExpired(expiresAt).ShouldBeTrue();
        sut.IsExpired(expiresAt.AddSeconds(1)).ShouldBeTrue();
    }

    // Given a newly created OTP code
    // When checking whether it has been used
    // Then it is not used
    [TestMethod]
    public void OtpCode_IsUsed_FalseInitially()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        sut.IsUsed.ShouldBeFalse();
    }

    // Given a newly created OTP code
    // When it is marked used at a given time
    // Then it becomes used and records that timestamp
    [TestMethod]
    public void OtpCode_MarkUsed_SetsUsedAtAndIsUsed()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));
        var now = DateTimeOffset.UtcNow;

        sut.MarkUsed(now);

        sut.IsUsed.ShouldBeTrue();
        sut.UsedAt.ShouldBe(now);
    }

    // Given an OTP code with four failed verification attempts
    // When checking whether it is locked
    // Then it is not locked
    [TestMethod]
    public void OtpCode_IsLocked_FalseBeforeFiveAttempts()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        for (var i = 0; i < 4; i++)
            sut.IncrementFailedAttempts();

        sut.IsLocked.ShouldBeFalse();
        sut.FailedAttempts.ShouldBe(4);
    }

    // Given an OTP code with five failed verification attempts
    // When checking whether it is locked
    // Then it is locked
    [TestMethod]
    public void OtpCode_IsLocked_TrueAfterFiveAttempts()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        for (var i = 0; i < 5; i++)
            sut.IncrementFailedAttempts();

        sut.IsLocked.ShouldBeTrue();
    }

    // Given a newly created OTP code
    // When checking whether it has been superseded
    // Then it is not superseded
    [TestMethod]
    public void OtpCode_IsSuperseded_FalseInitially()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));

        sut.IsSuperseded.ShouldBeFalse();
    }

    // Given a newly created OTP code
    // When it is superseded at a given time
    // Then it becomes superseded and records that timestamp
    [TestMethod]
    public void OtpCode_Supersede_SetsSupersededAt()
    {
        var sut = OtpCode.Create(DefaultTeamId, DefaultEventId, EventName.From("Event"), DefaultEmail, "000000",
            DateTimeOffset.UtcNow.AddMinutes(10));
        var now = DateTimeOffset.UtcNow;

        sut.Supersede(now);

        sut.IsSuperseded.ShouldBeTrue();
        sut.SupersededAt.ShouldBe(now);
    }
}
