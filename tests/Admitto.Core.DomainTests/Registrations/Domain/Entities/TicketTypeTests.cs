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
    private static TicketType CreateTicketType(int? maxCapacity = 10, int usedCapacity = 0)
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity);
        var tt = catalog.GetTicketType(id)!;
        for (var i = 0; i < usedCapacity; i++)
            tt.ClaimUncapped();
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
    // When a claim with enforcement is attempted
    // Then it throws a waitlist mode business rule violation
    [TestMethod]
    public void ClaimWithEnforcement_WhenWaitlistModeActive_ThrowsWaitlistModeError()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 1, waitlistEnabled: true);
        catalog.Claim([id], enforce: true);
        var sut = catalog.GetTicketType(id)!;

        Should.Throw<BusinessRuleViolationException>(() => sut.ClaimWithEnforcement())
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
