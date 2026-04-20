using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class TicketedEventLifecycleGuardTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();

    [TestMethod]
    public void SC001_Guard_Create_DefaultsToActiveAndZeroCount()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);

        sut.LifecycleStatus.ShouldBe(EventLifecycleStatus.Active);
        sut.PolicyMutationCount.ShouldBe(0);
        sut.IsActive.ShouldBeTrue();
    }

    [TestMethod]
    public void SC002_Guard_AssertActive_OnActive_IncrementsCount()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);

        sut.AssertActiveAndRegisterPolicyMutation();

        sut.PolicyMutationCount.ShouldBe(1);
    }

    [TestMethod]
    public void SC003_Guard_AssertActive_OnActive_IncrementsTwice()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);

        sut.AssertActiveAndRegisterPolicyMutation();
        sut.AssertActiveAndRegisterPolicyMutation();

        sut.PolicyMutationCount.ShouldBe(2);
    }

    [TestMethod]
    public void SC004_Guard_AssertActive_OnCancelled_Throws()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);
        sut.SetCancelled();

        var result = ErrorResult.Capture(() => sut.AssertActiveAndRegisterPolicyMutation());

        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC005_Guard_AssertActive_OnArchived_Throws()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);
        sut.SetArchived();

        var result = ErrorResult.Capture(() => sut.AssertActiveAndRegisterPolicyMutation());

        result.Error.ShouldMatch(TicketedEventLifecycleGuard.Errors.EventNotActive);
    }

    [TestMethod]
    public void SC006_Guard_SetCancelled_FromActive_TransitionsAndBumps()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);

        sut.SetCancelled();

        sut.LifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
        sut.PolicyMutationCount.ShouldBe(1);
        sut.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void SC007_Guard_SetCancelled_Idempotent_DoesNotBump()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);
        sut.SetCancelled();
        sut.PolicyMutationCount.ShouldBe(1);

        sut.SetCancelled();

        sut.LifecycleStatus.ShouldBe(EventLifecycleStatus.Cancelled);
        sut.PolicyMutationCount.ShouldBe(1);
    }

    [TestMethod]
    public void SC008_Guard_SetArchived_FromActive_TransitionsAndBumps()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);

        sut.SetArchived();

        sut.LifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
        sut.PolicyMutationCount.ShouldBe(1);
        sut.IsActive.ShouldBeFalse();
    }

    [TestMethod]
    public void SC009_Guard_SetArchived_FromCancelled_TransitionsAndBumps()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);
        sut.SetCancelled();
        sut.PolicyMutationCount.ShouldBe(1);

        sut.SetArchived();

        sut.LifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
        sut.PolicyMutationCount.ShouldBe(2);
    }

    [TestMethod]
    public void SC010_Guard_SetArchived_Idempotent_DoesNotBump()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);
        sut.SetArchived();
        sut.PolicyMutationCount.ShouldBe(1);

        sut.SetArchived();

        sut.LifecycleStatus.ShouldBe(EventLifecycleStatus.Archived);
        sut.PolicyMutationCount.ShouldBe(1);
    }

    [TestMethod]
    public void SC011_Guard_PolicyMutation_ThenLifecycleTransition_CountReflectsBoth()
    {
        var sut = TicketedEventLifecycleGuard.Create(DefaultEventId);
        sut.AssertActiveAndRegisterPolicyMutation(); // count=1
        sut.AssertActiveAndRegisterPolicyMutation(); // count=2
        sut.AssertActiveAndRegisterPolicyMutation(); // count=3

        sut.SetCancelled(); // count=4

        sut.PolicyMutationCount.ShouldBe(4);
    }
}
