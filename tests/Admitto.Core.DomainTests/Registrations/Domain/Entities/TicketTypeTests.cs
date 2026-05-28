using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
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

    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsPositive_Decrements()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 5);

        sut.ReleaseCapacity();

        sut.UsedCapacity.ShouldBe(4);
    }

    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsOne_DecrementsToZero()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 1);

        sut.ReleaseCapacity();

        sut.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void ReleaseCapacity_WhenUsedIsZero_ClampsAtZero()
    {
        var sut = CreateTicketType(maxCapacity: 10, usedCapacity: 0);

        sut.ReleaseCapacity();

        sut.UsedCapacity.ShouldBe(0);
    }

    [TestMethod]
    public void ClaimWithEnforcement_WhenWaitlistModeActive_ThrowsWaitlistModeError()
    {
        var id = TicketTypeId.New();
        var catalog = TicketCatalog.Create(TicketedEventId.New(), TeamId.New());
        catalog.AddTicketType(id, TicketTypeName.From("General"), [], maxCapacity: 1, waitlistEnabled: true);
        catalog.Claim([id], enforce: true);
        var sut = catalog.GetTicketType(id)!;

        Should.Throw<BusinessRuleViolationException>(() => sut.ClaimWithEnforcement())
            .Error.Code.ShouldBe("ticket_type.waitlist_mode");
    }

    [TestMethod]
    public void UpdateMaxReconfirmAttempts_ValidValue_SetsProperty()
    {
        var ticketType = CreateTicketType();

        ticketType.UpdateMaxReconfirmAttempts(3);

        ticketType.MaxReconfirmAttempts.ShouldBe(3);
    }

    [TestMethod]
    public void UpdateMaxReconfirmAttempts_Zero_ThrowsValidationError()
    {
        var ticketType = CreateTicketType();

        var ex = Should.Throw<BusinessRuleViolationException>(() => ticketType.UpdateMaxReconfirmAttempts(0));

        ex.Error.Code.ShouldBe("ticket_type.max_reconfirm_attempts_below_minimum");
    }

    [TestMethod]
    public void UpdateMaxReconfirmAttempts_Null_DisablesAutoCancelForType()
    {
        var ticketType = CreateTicketType();
        ticketType.UpdateMaxReconfirmAttempts(3);

        ticketType.UpdateMaxReconfirmAttempts(null);

        ticketType.MaxReconfirmAttempts.ShouldBeNull();
    }
}
