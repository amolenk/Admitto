using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class CancellationPolicyTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly DateTimeOffset Cutoff = new(2025, 5, 25, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void SC001_CancellationPolicy_BeforeCutoff_IsOnTime()
    {
        var sut = CancellationPolicy.Create(DefaultEventId, Cutoff);
        var cancellationInstant = Cutoff.AddDays(-5);

        sut.IsLateCancellation(cancellationInstant).ShouldBeFalse();
    }

    [TestMethod]
    public void SC002_CancellationPolicy_AtCutoff_IsLate()
    {
        var sut = CancellationPolicy.Create(DefaultEventId, Cutoff);

        sut.IsLateCancellation(Cutoff).ShouldBeTrue();
    }

    [TestMethod]
    public void SC003_CancellationPolicy_AfterCutoff_IsLate()
    {
        var sut = CancellationPolicy.Create(DefaultEventId, Cutoff);
        var cancellationInstant = Cutoff.AddDays(3);

        sut.IsLateCancellation(cancellationInstant).ShouldBeTrue();
    }

    [TestMethod]
    public void SC004_CancellationPolicy_UpdateCutoff_ChangesClassification()
    {
        var sut = CancellationPolicy.Create(DefaultEventId, Cutoff);
        var newCutoff = Cutoff.AddDays(-10);

        sut.UpdateCutoff(newCutoff);

        sut.LateCancellationCutoff.ShouldBe(newCutoff);
        // A time between old and new cutoff is now late
        sut.IsLateCancellation(Cutoff.AddDays(-5)).ShouldBeTrue();
    }
}
