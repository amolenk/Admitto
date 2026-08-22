using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureWaitlistPolicy;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ConfigureWaitlistPolicy;

[TestClass]
public sealed class ConfigureWaitlistPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active ticketed event
    // When the waitlist policy is configured with new quiet hours
    // Then the quiet hours are persisted on the event
    [TestMethod]
    public async ValueTask ConfigureWaitlistPolicy_ActiveEvent_PersistsPolicy()
    {
        var fixture = ConfigureWaitlistPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureWaitlistPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            new TimeOnly(23, 0),
            new TimeOnly(7, 0));

        var sut = new ConfigureWaitlistPolicyHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.WaitlistPolicy.QuietHoursStart.ShouldBe(new TimeOnly(23, 0));
            te.WaitlistPolicy.QuietHoursEnd.ShouldBe(new TimeOnly(7, 0));
        });
    }

    // Given an archived ticketed event
    // When the waitlist policy is configured
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask ConfigureWaitlistPolicy_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureWaitlistPolicyFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureWaitlistPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            new TimeOnly(23, 0),
            new TimeOnly(7, 0));

        var sut = new ConfigureWaitlistPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }

    // Given an active ticketed event
    // When the waitlist policy is configured with a stale version number
    // Then it fails with a concurrency conflict error
    [TestMethod]
    public async ValueTask ConfigureWaitlistPolicy_VersionMismatch_ThrowsConcurrencyConflict()
    {
        var fixture = ConfigureWaitlistPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureWaitlistPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion + 99u,
            new TimeOnly(23, 0),
            new TimeOnly(7, 0));

        var sut = new ConfigureWaitlistPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ConcurrencyConflictError.Create(fixture.SeededVersion + 99u, fixture.SeededVersion));
    }
}
