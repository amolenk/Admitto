using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Core.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class TicketTypeTests
{
    private static TicketType CreateTicketType(int? publicCapacity = 10, int usedCapacity = 0)
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity);
        var tt = catalog.GetTicketType(id)!;
        for (var i = 0; i < usedCapacity; i++)
            tt.Claim(ClaimMode.Public);
        return tt;
    }

    // Given a ticket type with several used slots
    // When capacity is released
    // Then the used capacity decrements by one
    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsPositive_Decrements()
    {
        var sut = CreateTicketType(publicCapacity: 10, usedCapacity: 5);

        sut.ReleaseCapacity();

        sut.PublicUsedCapacity.ShouldBe(4);
    }

    // Given a ticket type with exactly one used slot
    // When capacity is released
    // Then the used capacity decrements to zero
    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsOne_DecrementsToZero()
    {
        var sut = CreateTicketType(publicCapacity: 10, usedCapacity: 1);

        sut.ReleaseCapacity();

        sut.PublicUsedCapacity.ShouldBe(0);
    }

    // Given a ticket type with no used slots
    // When capacity is released
    // Then the used capacity stays clamped at zero
    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsZero_ClampsAtZero()
    {
        var sut = CreateTicketType(publicCapacity: 10, usedCapacity: 0);

        sut.ReleaseCapacity();

        sut.PublicUsedCapacity.ShouldBe(0);
    }

    // Given a waitlist-enabled ticket type that is already fully claimed and in waitlist mode
    // When a public claim is attempted
    // Then it throws a waitlist mode business rule violation
    [TestMethod]
    public void Claim_PublicWhenWaitlistModeActive_ThrowsWaitlistModeError()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], publicCapacity: 1, waitlistEnabled: true);
        catalog.Claim([id], ClaimMode.Public);
        var sut = catalog.GetTicketType(id)!;

        Should.Throw<BusinessRuleViolationException>(() => sut.Claim(ClaimMode.Public))
            .Error.ShouldMatch(TicketType.Errors.TicketTypeInWaitlistMode(id));
    }

    // Given a bounded-capacity ticket type that is fully claimed
    // When checking whether it is sold out
    // Then it returns true
    [TestMethod]
    public void IsSoldOut_WhenBoundedAndAtCapacity_ReturnsTrue()
    {
        var sut = CreateTicketType(publicCapacity: 10, usedCapacity: 10);

        sut.IsSoldOut.ShouldBeTrue();
    }

    // Given a bounded-capacity ticket type with capacity remaining
    // When checking whether it is sold out
    // Then it returns false
    [TestMethod]
    public void IsSoldOut_WhenBoundedAndUnderCapacity_ReturnsFalse()
    {
        var sut = CreateTicketType(publicCapacity: 10, usedCapacity: 9);

        sut.IsSoldOut.ShouldBeFalse();
    }

    // Given a ticket type with unlimited (null) capacity
    // When checking whether it is sold out
    // Then it returns false
    [TestMethod]
    public void IsSoldOut_WhenCapacityIsNull_ReturnsFalse()
    {
        var sut = CreateTicketType(publicCapacity: null, usedCapacity: 10);

        sut.IsSoldOut.ShouldBeFalse();
    }

    // Given a ticket type whose public seats are all used
    // When an admin claims a ticket
    // Then the admin count increments and the public counters are untouched
    [TestMethod]
    public void Claim_AdminOnSoldOut_IncrementsAdminCountOnly()
    {
        var sut = CreateTicketType(publicCapacity: 2, usedCapacity: 2);

        sut.Claim(ClaimMode.Admin);

        sut.AdminUsedCount.ShouldBe(1);
        sut.PublicUsedCapacity.ShouldBe(2);
        sut.AvailableCapacity.ShouldBe(0);
    }

    // Given admin tickets claimed before any public claims
    // When public claims fill the public capacity
    // Then it becomes sold out at exactly the public capacity, not before
    [TestMethod]
    public void IsSoldOut_AdminClaimedEarlyThenPublicFillsCapacity_SoldOutAtPublicCapacity()
    {
        var sut = CreateTicketType(publicCapacity: 200);

        for (var i = 0; i < 10; i++)
            sut.Claim(ClaimMode.Admin); // early admin claims

        for (var i = 0; i < 199; i++)
            sut.Claim(ClaimMode.Public);
        sut.IsSoldOut.ShouldBeFalse();

        sut.Claim(ClaimMode.Public); // 200th public claim exhausts the public capacity

        sut.IsSoldOut.ShouldBeTrue();
    }

    // Given a public ticket and an admin ticket
    // When the admin ticket is released
    // Then only the admin count decrements and no public seat is freed
    [TestMethod]
    public void ReleaseCapacity_AdminMode_DecrementsAdminCountOnly()
    {
        var sut = CreateTicketType(publicCapacity: 1, usedCapacity: 1);
        sut.Claim(ClaimMode.Admin);

        sut.ReleaseCapacity(ClaimMode.Admin);

        sut.AdminUsedCount.ShouldBe(0);
        sut.PublicUsedCapacity.ShouldBe(1);
        sut.PublicAvailableCapacity.ShouldBe(0);
    }

    // Given a public ticket and an admin ticket
    // When capacity is released with the default (public) mode
    // Then the admin count is left untouched
    [TestMethod]
    public void ReleaseCapacity_DefaultMode_DoesNotAffectAdminUsedCount()
    {
        var sut = CreateTicketType(publicCapacity: 10);
        sut.Claim(ClaimMode.Admin);
        sut.Claim(ClaimMode.Public);

        sut.ReleaseCapacity();

        sut.AdminUsedCount.ShouldBe(1);
        sut.PublicUsedCapacity.ShouldBe(0);
    }

    // Given a newly created ticket type
    // When the maximum reconfirmation emails is updated to a valid value
    // Then the property reflects the new value
    [TestMethod]
    public void UpdateMaxReconfirmationEmails_ValidValue_SetsProperty()
    {
        var ticketType = CreateTicketType();

        ticketType.UpdateMaxReconfirmationEmails(ReconfirmationEmailLimit.From(3));

        ticketType.MaxReconfirmationEmails!.Value.Value.ShouldBe(3);
    }

    // Given a maximum reconfirmation email limit
    // When zero is parsed as the limit
    // Then parsing fails validation
    [TestMethod]
    public void ReconfirmationEmailLimit_Zero_FailsValidation()
    {
        var ticketType = CreateTicketType();

        var result = ReconfirmationEmailLimit.TryFrom(0);

        result.IsSuccess.ShouldBeFalse();
    }

    // Given a ticket type with a maximum reconfirmation emails value already set
    // When the maximum reconfirmation emails is updated to null
    // Then the property becomes null, disabling auto-cancel for the type
    [TestMethod]
    public void UpdateMaxReconfirmationEmails_Null_DisablesAutoCancelForType()
    {
        var ticketType = CreateTicketType();
        ticketType.UpdateMaxReconfirmationEmails(ReconfirmationEmailLimit.From(3));

        ticketType.UpdateMaxReconfirmationEmails(null);

        ticketType.MaxReconfirmationEmails.ShouldBeNull();
    }
}
