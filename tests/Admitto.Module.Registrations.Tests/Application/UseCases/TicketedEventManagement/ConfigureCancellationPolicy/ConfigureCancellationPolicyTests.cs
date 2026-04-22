using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ConfigureCancellationPolicy;

[TestClass]
public sealed class ConfigureCancellationPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_ConfigureCancellationPolicy_ActiveEvent_PersistsPolicy()
    {
        var fixture = ConfigureCancellationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var cutoff = DateTimeOffset.UtcNow.AddDays(20);

        var sut = new ConfigureCancellationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(
            new ConfigureCancellationPolicyCommand(fixture.EventId, fixture.SeededVersion, cutoff),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.CancellationPolicy.ShouldNotBeNull();
            te.CancellationPolicy.LateCancellationCutoff.ShouldBe(cutoff);
        });
    }

    [TestMethod]
    public async ValueTask SC002_ConfigureCancellationPolicy_ClearExistingPolicy_RemovesPolicy()
    {
        var fixture = ConfigureCancellationPolicyFixture.ActiveWithExistingPolicy();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureCancellationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(
            new ConfigureCancellationPolicyCommand(fixture.EventId, fixture.SeededVersion, LateCancellationCutoff: null),
            testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.CancellationPolicy.ShouldBeNull();
        });
    }

    [TestMethod]
    public async ValueTask SC003_ConfigureCancellationPolicy_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureCancellationPolicyFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var sut = new ConfigureCancellationPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(
                new ConfigureCancellationPolicyCommand(
                    fixture.EventId,
                    fixture.SeededVersion,
                    DateTimeOffset.UtcNow.AddDays(20)),
                testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }
}
