using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;

[TestClass]
public sealed class ConfigureRegistrationPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    [TestMethod]
    public async ValueTask SC001_ConfigureRegistrationPolicy_ActiveEvent_PersistsPolicy()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = DateTimeOffset.UtcNow.AddDays(1);
        var closesAt = DateTimeOffset.UtcNow.AddDays(10);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId,
            fixture.SeededVersion,
            opensAt,
            closesAt,
            "@example.com");

        var sut = new ConfigureRegistrationPolicyHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.RegistrationPolicy.ShouldNotBeNull();
            te.RegistrationPolicy.OpensAt.ShouldBe(opensAt);
            te.RegistrationPolicy.ClosesAt.ShouldBe(closesAt);
            te.RegistrationPolicy.AllowedEmailDomain.ShouldBe("@example.com");
        });
    }

    [TestMethod]
    public async ValueTask SC002_ConfigureRegistrationPolicy_CancelledEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureRegistrationPolicyFixture.CancelledEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId,
            fixture.SeededVersion,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(10),
            null);

        var sut = new ConfigureRegistrationPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public async ValueTask SC003_ConfigureRegistrationPolicy_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId,
            fixture.SeededVersion,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(10),
            null);

        var sut = new ConfigureRegistrationPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("ticketed_event.event_not_active");
    }

    [TestMethod]
    public async ValueTask SC004_ConfigureRegistrationPolicy_VersionMismatch_ThrowsConcurrencyConflict()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId,
            fixture.SeededVersion + 99u,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(10),
            null);

        var sut = new ConfigureRegistrationPolicyHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.Code.ShouldBe("concurrency_conflict");
    }
}
