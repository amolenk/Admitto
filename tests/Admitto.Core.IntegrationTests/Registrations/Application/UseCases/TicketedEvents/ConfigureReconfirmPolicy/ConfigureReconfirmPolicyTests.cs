using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureReconfirmPolicy;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ConfigureReconfirmPolicy;

[TestClass]
public sealed class ConfigureReconfirmPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active ticketed event
    // When a reconfirm policy with dates, an interval, and quiet hours is configured
    // Then the policy is persisted on the event
    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_ActiveEvent_PersistsPolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);
        var opensAt = DateTimeOffset.UtcNow.AddDays(5);
        var closesAt = DateTimeOffset.UtcNow.AddDays(15);
        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(new ConfigureReconfirmPolicyCommand(
            fixture.EventId.Value, fixture.TeamId.Value, fixture.SeededVersion,
            opensAt, closesAt, 24, new TimeOnly(22), new TimeOnly(8)),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents.FirstOrDefaultAsync(
                e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.ReconfirmPolicy.ShouldNotBeNull();
            te.ReconfirmPolicy.OpensAt.ShouldBe(opensAt);
            te.ReconfirmPolicy.ClosesAt.ShouldBe(closesAt);
            te.ReconfirmPolicy.MinEmailInterval.ShouldBe(TimeSpan.FromHours(24));
            te.ReconfirmPolicy.QuietHoursStart.ShouldBe(new TimeOnly(22));
            te.ReconfirmPolicy.QuietHoursEnd.ShouldBe(new TimeOnly(8));
        });
    }

    // Given an active event with an existing reconfirm policy
    // When all policy fields are cleared
    // Then the reconfirm policy is removed from the event
    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_ClearExistingPolicy_RemovesPolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveWithExistingPolicy();
        await fixture.SetupAsync(Environment);
        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(new ConfigureReconfirmPolicyCommand(
            fixture.EventId.Value, fixture.TeamId.Value, fixture.SeededVersion,
            null, null, null, null, null), testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents.FirstOrDefaultAsync(
                e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.ReconfirmPolicy.ShouldBeNull();
        });
    }

    // Given an archived ticketed event
    // When a reconfirm policy is configured
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);
        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(() => sut.HandleAsync(
            new ConfigureReconfirmPolicyCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.SeededVersion,
                DateTimeOffset.UtcNow.AddDays(5), DateTimeOffset.UtcNow.AddDays(15),
                24, null, null), testContext.CancellationToken));

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }

    // Given an active ticketed event
    // When a policy is configured with only an opening date
    // Then it fails with an incomplete-policy error
    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_IncompleteFields_ThrowsIncompletePolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);
        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(() => sut.HandleAsync(
            new ConfigureReconfirmPolicyCommand(
                fixture.EventId.Value, fixture.TeamId.Value, fixture.SeededVersion,
                DateTimeOffset.UtcNow.AddDays(5), null, 24, null, null),
            testContext.CancellationToken));

        result.Error.ShouldMatch(ConfigureReconfirmPolicyHandler.Errors.IncompletePolicy);
    }
}
