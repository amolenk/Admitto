using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

[TestClass]
public sealed class ConfigureReconfirmPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_ActiveEvent_PersistsPolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = DateTimeOffset.UtcNow.AddDays(5);
        var closesAt = DateTimeOffset.UtcNow.AddDays(15);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(
            new ConfigureReconfirmPolicyCommand(
                fixture.EventId.Value, fixture.SeededVersion, opensAt, closesAt, CadenceHours: 7, MinEmailIntervalHours: 24),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.ReconfirmPolicy.ShouldNotBeNull();
            te.ReconfirmPolicy.OpensAt.ShouldBe(opensAt);
            te.ReconfirmPolicy.ClosesAt.ShouldBe(closesAt);
            te.ReconfirmPolicy.Cadence.ShouldBe(TimeSpan.FromHours(7));
            te.ReconfirmPolicy.MinEmailInterval.ShouldBe(TimeSpan.FromHours(24));
        });
    }

    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_ClearExistingPolicy_RemovesPolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveWithExistingPolicy();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(
            new ConfigureReconfirmPolicyCommand(
                fixture.EventId.Value, fixture.SeededVersion, OpensAt: null, ClosesAt: null, CadenceHours: null, MinEmailIntervalHours: null),
            testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.ReconfirmPolicy.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(
                new ConfigureReconfirmPolicyCommand(
                    fixture.EventId.Value,
                    fixture.SeededVersion,
                    DateTimeOffset.UtcNow.AddDays(5),
                    DateTimeOffset.UtcNow.AddDays(15),
                    CadenceHours: 7,
                    MinEmailIntervalHours: 24),
                testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public async ValueTask ConfigureReconfirmPolicy_IncompleteFields_ThrowsIncompletePolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(
                new ConfigureReconfirmPolicyCommand(
                    fixture.EventId.Value,
                    fixture.SeededVersion,
                    OpensAt: DateTimeOffset.UtcNow.AddDays(5),
                    ClosesAt: null,
                    CadenceHours: 7,
                    MinEmailIntervalHours: null),
                testContext.CancellationToken));

        result.Error.Code.ShouldBe("configure_reconfirm_policy.incomplete");
    }
}
