using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Shouldly;

namespace Amolenk.Admitto.Module.Registrations.Domain.Tests.Entities;

[TestClass]
public sealed class EventRegistrationPolicyTests
{
    private static readonly TicketedEventId DefaultEventId = TicketedEventId.New();
    private static readonly DateTimeOffset Now = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void SC001_EventRegistrationPolicy_SetWindow_ValidRange_SetsWindow()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        var opensAt = Now;
        var closesAt = Now.AddDays(7);

        // Act
        sut.SetWindow(opensAt, closesAt);

        // Assert
        sut.RegistrationWindowOpensAt.ShouldBe(opensAt);
        sut.RegistrationWindowClosesAt.ShouldBe(closesAt);
        sut.HasRegistrationWindow.ShouldBeTrue();
    }

    [TestMethod]
    public void SC002_EventRegistrationPolicy_SetWindow_CloseBeforeOpen_Throws()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        var opensAt = Now.AddDays(1);
        var closesAt = Now; // before open

        // Act
        var result = ErrorResult.Capture(() => sut.SetWindow(opensAt, closesAt));

        // Assert
        result.Error.ShouldMatch(EventRegistrationPolicy.Errors.WindowCloseBeforeOpen);
    }

    [TestMethod]
    public void SC003_EventRegistrationPolicy_IsRegistrationOpen_WithinWindow_ReturnsTrue()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetWindow(Now.AddDays(-1), Now.AddDays(1));

        // Act & Assert
        sut.IsRegistrationOpen(Now).ShouldBeTrue();
    }

    [TestMethod]
    public void SC004_EventRegistrationPolicy_IsRegistrationOpen_BeforeWindow_ReturnsFalse()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetWindow(Now.AddDays(1), Now.AddDays(7));

        // Act & Assert
        sut.IsRegistrationOpen(Now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC005_EventRegistrationPolicy_IsRegistrationOpen_AfterWindow_ReturnsFalse()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetWindow(Now.AddDays(-7), Now.AddDays(-1));

        // Act & Assert
        sut.IsRegistrationOpen(Now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC006_EventRegistrationPolicy_IsRegistrationOpen_NoWindow_ReturnsFalse()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);

        // Act & Assert
        sut.IsRegistrationOpen(Now).ShouldBeFalse();
    }

    [TestMethod]
    public void SC007_EventRegistrationPolicy_IsEmailDomainAllowed_NoRestriction_ReturnsTrue()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);

        // Act & Assert
        sut.IsEmailDomainAllowed("anyone@example.com").ShouldBeTrue();
    }

    [TestMethod]
    public void SC008_EventRegistrationPolicy_IsEmailDomainAllowed_MatchingDomain_ReturnsTrue()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetDomainRestriction("@contoso.com");

        // Act & Assert
        sut.IsEmailDomainAllowed("alice@contoso.com").ShouldBeTrue();
    }

    [TestMethod]
    public void SC009_EventRegistrationPolicy_IsEmailDomainAllowed_NonMatchingDomain_ReturnsFalse()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetDomainRestriction("@contoso.com");

        // Act & Assert
        sut.IsEmailDomainAllowed("alice@other.com").ShouldBeFalse();
    }

    [TestMethod]
    public void SC010_EventRegistrationPolicy_SetDomainRestriction_Null_ClearsRestriction()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetDomainRestriction("@contoso.com");

        // Act
        sut.SetDomainRestriction(null);

        // Assert
        sut.AllowedEmailDomain.ShouldBeNull();
        sut.IsEmailDomainAllowed("anyone@example.com").ShouldBeTrue();
    }

    [TestMethod]
    public void SC011_EventRegistrationPolicy_ClearWindow_RemovesWindow()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetWindow(Now, Now.AddDays(7));
        sut.HasRegistrationWindow.ShouldBeTrue();

        // Act
        sut.ClearWindow();

        // Assert
        sut.RegistrationWindowOpensAt.ShouldBeNull();
        sut.RegistrationWindowClosesAt.ShouldBeNull();
        sut.HasRegistrationWindow.ShouldBeFalse();
    }

    [TestMethod]
    public void SC012_EventRegistrationPolicy_DefaultLifecycleStatus_IsActive()
    {
        // Act
        var sut = EventRegistrationPolicy.Create(DefaultEventId);

        // Assert
        sut.EventLifecycleStatus.ShouldBe(ValueObjects.EventLifecycleStatus.Active);
        sut.IsEventActive.ShouldBeTrue();
    }

    [TestMethod]
    public void SC013_EventRegistrationPolicy_SetCancelled_UpdatesStatus()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);

        // Act
        sut.SetCancelled();

        // Assert
        sut.EventLifecycleStatus.ShouldBe(ValueObjects.EventLifecycleStatus.Cancelled);
        sut.IsEventActive.ShouldBeFalse();
    }

    [TestMethod]
    public void SC014_EventRegistrationPolicy_SetArchived_UpdatesStatus()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);

        // Act
        sut.SetArchived();

        // Assert
        sut.EventLifecycleStatus.ShouldBe(ValueObjects.EventLifecycleStatus.Archived);
        sut.IsEventActive.ShouldBeFalse();
    }

    [TestMethod]
    public void SC015_EventRegistrationPolicy_SetCancelled_Idempotent()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetCancelled();

        // Act (second call — should be idempotent)
        sut.SetCancelled();

        // Assert
        sut.EventLifecycleStatus.ShouldBe(ValueObjects.EventLifecycleStatus.Cancelled);
    }

    [TestMethod]
    public void SC016_EventRegistrationPolicy_SetArchived_Idempotent()
    {
        // Arrange
        var sut = EventRegistrationPolicy.Create(DefaultEventId);
        sut.SetArchived();

        // Act (second call — should be idempotent)
        sut.SetArchived();

        // Assert
        sut.EventLifecycleStatus.ShouldBe(ValueObjects.EventLifecycleStatus.Archived);
    }
}
