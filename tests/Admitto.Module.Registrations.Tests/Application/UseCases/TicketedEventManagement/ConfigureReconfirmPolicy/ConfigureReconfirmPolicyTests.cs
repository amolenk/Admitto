using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

[TestClass]
public sealed class ConfigureReconfirmPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_ConfigureReconfirmPolicy_ActiveEvent_PersistsPolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = DateTimeOffset.UtcNow.AddDays(5);
        var closesAt = DateTimeOffset.UtcNow.AddDays(15);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(
            new ConfigureReconfirmPolicyCommand(
                fixture.EventId, fixture.SeededVersion, opensAt, closesAt, CadenceDays: 7),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.ReconfirmPolicy.ShouldNotBeNull();
            te.ReconfirmPolicy.OpensAt.ShouldBe(opensAt);
            te.ReconfirmPolicy.ClosesAt.ShouldBe(closesAt);
            te.ReconfirmPolicy.Cadence.ShouldBe(TimeSpan.FromDays(7));
        });
    }

    [TestMethod]
    public async ValueTask SC002_ConfigureReconfirmPolicy_ClearExistingPolicy_RemovesPolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveWithExistingPolicy();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(
            new ConfigureReconfirmPolicyCommand(
                fixture.EventId, fixture.SeededVersion, OpensAt: null, ClosesAt: null, CadenceDays: null),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.ReconfirmPolicy.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask SC003_ConfigureReconfirmPolicy_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureReconfirmPolicyFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(
                new ConfigureReconfirmPolicyCommand(
                    fixture.EventId,
                    fixture.SeededVersion,
                    DateTimeOffset.UtcNow.AddDays(5),
                    DateTimeOffset.UtcNow.AddDays(15),
                    CadenceDays: 7),
                testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public async ValueTask SC004_ConfigureReconfirmPolicy_IncompleteFields_ThrowsIncompletePolicy()
    {
        var fixture = ConfigureReconfirmPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureReconfirmPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(
                new ConfigureReconfirmPolicyCommand(
                    fixture.EventId,
                    fixture.SeededVersion,
                    OpensAt: DateTimeOffset.UtcNow.AddDays(5),
                    ClosesAt: null,
                    CadenceDays: 7),
                testContext.CancellationToken));

        result.Error.Code.ShouldBe("configure_reconfirm_policy.incomplete");
    }
}
