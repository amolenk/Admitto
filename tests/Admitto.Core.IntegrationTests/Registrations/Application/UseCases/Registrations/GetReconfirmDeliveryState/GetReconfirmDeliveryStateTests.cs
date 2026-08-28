using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.GetReconfirmDeliveryState;

[TestClass]
public sealed class GetReconfirmDeliveryStateTests : AspireIntegrationTestBase
{
    // Given an active event and a queued registration snapshot
    // When the authoritative reconfirm delivery query runs
    // Then it returns all data required for Email eligibility
    [TestMethod]
    public async ValueTask GetReconfirmDeliveryState_QueuedSnapshot_ReturnsAuthoritativeDeliveryState()
    {
        var fixture = GetReconfirmDeliveryStateFixture.Active(DateTimeOffset.UtcNow);
        await fixture.SetupAsync(Environment);

        var state = await fixture.QueryAsync(Environment);
        var allowed = state.ShouldBeOfType<ReconfirmDeliveryState.Allowed>();

        allowed.RegistrationCreatedAt.ShouldNotBe(default);
        allowed.MinimumEmailInterval.ShouldBe(TimeSpan.FromHours(24));
        allowed.EffectiveMaxReconfirmationEmails.ShouldBe(2);
        allowed.DeliveryCutoffAt.ShouldBeGreaterThan(fixture.Now);
    }

    // Given a queued registration that has reconfirmed before delivery
    // When the authoritative reconfirm delivery query runs
    // Then the delivery is suppressed
    [TestMethod]
    public async ValueTask GetReconfirmDeliveryState_RegistrationReconfirmedAfterQueue_SuppressesDelivery()
    {
        var fixture = GetReconfirmDeliveryStateFixture.Active(DateTimeOffset.UtcNow);
        await fixture.SetupAsync(Environment);
        await fixture.ReconfirmAsync(Environment);

        var state = await fixture.QueryAsync(Environment);

        state.ShouldBeOfType<ReconfirmDeliveryState.Suppressed>().Reason
            .ShouldBe(ReconfirmDeliverySuppression.RegistrationReconfirmed);
    }

    // Given a queued registration that has been cancelled before delivery
    // When the authoritative reconfirm delivery query runs
    // Then the delivery is suppressed
    [TestMethod]
    public async ValueTask GetReconfirmDeliveryState_RegistrationCancelledAfterQueue_SuppressesDelivery()
    {
        var fixture = GetReconfirmDeliveryStateFixture.Active(DateTimeOffset.UtcNow);
        await fixture.SetupAsync(Environment);
        await fixture.CancelAsync(Environment);

        var state = await fixture.QueryAsync(Environment);

        state.ShouldBeOfType<ReconfirmDeliveryState.Suppressed>().Reason
            .ShouldBe(ReconfirmDeliverySuppression.RegistrationCancelled);
    }

    // Given a queued registration whose event has since been archived
    // When the authoritative reconfirm delivery query runs
    // Then the delivery is suppressed
    [TestMethod]
    public async ValueTask GetReconfirmDeliveryState_EventArchivedAfterQueue_SuppressesDelivery()
    {
        var fixture = GetReconfirmDeliveryStateFixture.Active(DateTimeOffset.UtcNow);
        await fixture.SetupAsync(Environment);
        await fixture.ArchiveAsync(Environment);

        var state = await fixture.QueryAsync(Environment);

        state.ShouldBeOfType<ReconfirmDeliveryState.Suppressed>().Reason
            .ShouldBe(ReconfirmDeliverySuppression.EventNotActive);
    }

    // Given a queued registration whose reconfirm policy has been disabled
    // When the authoritative reconfirm delivery query runs
    // Then the delivery is suppressed
    [TestMethod]
    public async ValueTask GetReconfirmDeliveryState_PolicyDisabledAfterQueue_SuppressesDelivery()
    {
        var fixture = GetReconfirmDeliveryStateFixture.Active(DateTimeOffset.UtcNow);
        await fixture.SetupAsync(Environment);
        await fixture.DisablePolicyAsync(Environment);

        var state = await fixture.QueryAsync(Environment);

        state.ShouldBeOfType<ReconfirmDeliveryState.Suppressed>().Reason
            .ShouldBe(ReconfirmDeliverySuppression.PolicyDisabled);
    }

    // Given a queued registration whose reconfirm window has expired
    // When the authoritative reconfirm delivery query runs
    // Then the delivery is suppressed
    [TestMethod]
    public async ValueTask GetReconfirmDeliveryState_WindowExpiredAfterQueue_SuppressesDelivery()
    {
        var fixture = GetReconfirmDeliveryStateFixture.Active(DateTimeOffset.UtcNow);
        await fixture.SetupAsync(Environment);

        var state = await fixture.QueryAsync(Environment, fixture.Now.AddHours(3));

        state.ShouldBeOfType<ReconfirmDeliveryState.Suppressed>().Reason
            .ShouldBe(ReconfirmDeliverySuppression.OutsideWindow);
    }

    // Given a queued registration delivered during event-local quiet hours
    // When the authoritative reconfirm delivery query runs
    // Then the delivery is suppressed
    [TestMethod]
    public async ValueTask GetReconfirmDeliveryState_QuietHoursAtDelivery_SuppressesDelivery()
    {
        var fixture = GetReconfirmDeliveryStateFixture.Active(
            new DateTimeOffset(2030, 6, 1, 23, 0, 0, TimeSpan.Zero));
        await fixture.SetupAsync(Environment);
        await fixture.ConfigureQuietHoursAsync(Environment);

        var state = await fixture.QueryAsync(Environment);

        state.ShouldBeOfType<ReconfirmDeliveryState.Suppressed>().Reason
            .ShouldBe(ReconfirmDeliverySuppression.QuietHours);
    }
}
