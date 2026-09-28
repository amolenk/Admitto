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
    private static TicketType CreateTicketType(int? maxCapacity = 10, int usedCapacity = 0, int reservedCapacity = 0)
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity, reservedCapacity: reservedCapacity);
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
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 5);

        sut.ReleaseCapacity();

        sut.UsedCapacity.ShouldBe(4);
    }

    // Given a ticket type with exactly one used slot
    // When capacity is released
    // Then the used capacity decrements to zero
    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsOne_DecrementsToZero()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 1);

        sut.ReleaseCapacity();

        sut.UsedCapacity.ShouldBe(0);
    }

    // Given a ticket type with no used slots
    // When capacity is released
    // Then the used capacity stays clamped at zero
    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsZero_ClampsAtZero()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 0);

        sut.ReleaseCapacity();

        sut.UsedCapacity.ShouldBe(0);
    }

    // Given a waitlist-enabled ticket type that is already fully claimed and in waitlist mode
    // When a public claim is attempted
    // Then it throws a waitlist mode business rule violation
    [TestMethod]
    public void Claim_PublicWhenWaitlistModeActive_ThrowsWaitlistModeError()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 1, waitlistEnabled: true);
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
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 10);

        sut.IsSoldOut.ShouldBeTrue();
    }

    // Given a bounded-capacity ticket type with capacity remaining
    // When checking whether it is sold out
    // Then it returns false
    [TestMethod]
    public void IsSoldOut_WhenBoundedAndUnderCapacity_ReturnsFalse()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 9);

        sut.IsSoldOut.ShouldBeFalse();
    }

    // Given a ticket type with unlimited (null) capacity
    // When checking whether it is sold out
    // Then it returns false
    [TestMethod]
    public void IsSoldOut_WhenCapacityIsNull_ReturnsFalse()
    {
        var sut = CreateTicketType(maxCapacity: null, usedCapacity: 10);

        sut.IsSoldOut.ShouldBeFalse();
    }

    // Given a ticket type with reserved capacity and used capacity at the public threshold
    // When checking whether it is sold out
    // Then it returns true even though total capacity has not been reached
    [TestMethod]
    public void IsSoldOut_WhenUsedCapacityReachesPublicThreshold_ReturnsTrue()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 8, reservedCapacity: 2);

        sut.IsSoldOut.ShouldBeTrue();
    }

    // Given a ticket type with reserved capacity and used capacity below the public threshold
    // When checking whether it is sold out
    // Then it returns false
    [TestMethod]
    public void IsSoldOut_WhenUsedCapacityBelowPublicThreshold_ReturnsFalse()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 7, reservedCapacity: 2);

        sut.IsSoldOut.ShouldBeFalse();
    }

    // Given a ticket type with reserved capacity and an early reserved claim
    // When the reserved claim is made before any public claims
    // Then the reserved buffer is held back and full public capacity remains available
    [TestMethod]
    public void Claim_ReservedBeforeAnyPublicClaims_HoldsBackBufferFromPublicPool()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 200, reservedCapacity: 20);
        var sut = catalog.GetTicketType(id)!;

        for (var i = 0; i < 10; i++)
            sut.Claim(ClaimMode.Reserved);

        sut.PublicAvailableCapacity(sut.MaxCapacity, sut.ReservedCapacity).ShouldBe(180);
    }

    // Given a ticket type with reserved capacity partly consumed by an early reserved claim
    // When public claims fill the remaining public pool
    // Then it becomes sold out at exactly the public threshold, not before
    [TestMethod]
    public void IsSoldOut_ReservedClaimedEarlyThenPublicFillsRemainder_SoldOutAtPublicThreshold()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 200, reservedCapacity: 20);
        var sut = catalog.GetTicketType(id)!;

        for (var i = 0; i < 10; i++)
            sut.Claim(ClaimMode.Reserved); // early admin claims

        for (var i = 0; i < 179; i++)
            sut.Claim(ClaimMode.Public);
        sut.IsSoldOut.ShouldBeFalse();

        sut.Claim(ClaimMode.Public); // 180th public claim exhausts the public pool

        sut.IsSoldOut.ShouldBeTrue();
    }

    // Given reserved capacity of zero
    // When an admin claims a ticket
    // Then it simply consumes a public seat, matching pre-reserved-capacity behavior
    [TestMethod]
    public void Claim_ReservedWithZeroReservedCapacity_ConsumesPublicSeat()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 100);
        var sut = catalog.GetTicketType(id)!;

        sut.Claim(ClaimMode.Reserved);

        sut.PublicAvailableCapacity(sut.MaxCapacity, sut.ReservedCapacity).ShouldBe(99);
    }

    // Given reserved capacity fully consumed by reserved claims
    // When another reserved claim is made beyond the buffer
    // Then it spills into the public pool and IsSoldOut reflects the reduced availability
    [TestMethod]
    public void Claim_ReservedBeyondReservedCapacity_SpillsIntoPublicPool()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, reservedCapacity: 2);
        var sut = catalog.GetTicketType(id)!;

        sut.Claim(ClaimMode.Reserved);
        sut.Claim(ClaimMode.Reserved);
        sut.Claim(ClaimMode.Reserved); // 3rd reserved claim exceeds the buffer of 2

        sut.PublicAvailableCapacity(sut.MaxCapacity, sut.ReservedCapacity).ShouldBe(7);
    }

    // Given a reserved claim that has been released
    // When the reserved buffer is checked
    // Then the reserved slot is credited back and held from the public pool again
    [TestMethod]
    public void ReleaseCapacity_ReservedMode_CreditsReservedBufferBack()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, reservedCapacity: 2);
        var sut = catalog.GetTicketType(id)!;
        sut.Claim(ClaimMode.Reserved);

        sut.ReleaseCapacity(ClaimMode.Reserved);

        sut.ReservedUsedCapacity.ShouldBe(0);
        sut.PublicAvailableCapacity(sut.MaxCapacity, sut.ReservedCapacity).ShouldBe(8);
    }

    // Given a public claim that has been released
    // When capacity is released with the default (public) mode
    // Then the reserved buffer is left untouched
    [TestMethod]
    public void ReleaseCapacity_DefaultMode_DoesNotAffectReservedUsedCapacity()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 10, reservedCapacity: 2);
        var sut = catalog.GetTicketType(id)!;
        sut.Claim(ClaimMode.Reserved);
        sut.Claim(ClaimMode.Public);

        sut.ReleaseCapacity();

        sut.ReservedUsedCapacity.ShouldBe(1);
        sut.UsedCapacity.ShouldBe(1);
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
