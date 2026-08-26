using Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy;

[TestClass]
public sealed class ConfigureRegistrationPolicyTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active ticketed event
    // When a registration policy with open/close dates and an allowed email domain is configured
    // Then the policy is persisted on the event
    [TestMethod]
    public async ValueTask ConfigureRegistrationPolicy_ActiveEvent_PersistsPolicy()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var opensAt = DateTimeOffset.UtcNow.AddDays(1);
        var closesAt = DateTimeOffset.UtcNow.AddDays(10);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            opensAt,
            closesAt,
            "@example.com");

        var sut = new ConfigureRegistrationPolicyHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
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

    // Given an active event with an existing registration policy
    // When the policy fields are all cleared
    // Then the registration policy is removed from the event
    [TestMethod]
    public async ValueTask ConfigureRegistrationPolicy_ClearExistingPolicy_RemovesPolicy()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ActiveWithExistingPolicy();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            OpensAt: null,
            ClosesAt: null,
            AllowedEmailDomain: null);

        var sut = new ConfigureRegistrationPolicyHandler(Environment.RegistrationsDatabase.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async ctx =>
        {
            var te = await ctx.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == fixture.EventId, testContext.CancellationToken);
            te.ShouldNotBeNull();
            te.RegistrationPolicy.ShouldBeNull();
        });
    }

    // Given an active ticketed event
    // When the registration policy is configured with an opens date but no closes date
    // Then it fails with an incomplete-policy error
    [TestMethod]
    public async ValueTask ConfigureRegistrationPolicy_IncompleteFields_ThrowsIncompletePolicy()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            OpensAt: DateTimeOffset.UtcNow.AddDays(1),
            ClosesAt: null,
            AllowedEmailDomain: null);

        var sut = new ConfigureRegistrationPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ConfigureRegistrationPolicyHandler.Errors.IncompletePolicy);
    }

    // Given an archived ticketed event
    // When a registration policy is configured
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask ConfigureRegistrationPolicy_ArchivedEvent_ThrowsEventNotActive()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(10),
            null);

        var sut = new ConfigureRegistrationPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(TicketedEvent.Errors.EventNotActive);
    }

    // Given an active ticketed event
    // When the registration policy is configured with a stale version number
    // Then it fails with a concurrency conflict error
    [TestMethod]
    public async ValueTask ConfigureRegistrationPolicy_VersionMismatch_ThrowsConcurrencyConflict()
    {
        var fixture = ConfigureRegistrationPolicyFixture.ActiveEvent();
        await fixture.SetupAsync(Environment);

        var command = new ConfigureRegistrationPolicyCommand(
            fixture.EventId.Value,
            fixture.TeamId.Value,
            fixture.SeededVersion + 99u,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(10),
            null);

        var sut = new ConfigureRegistrationPolicyHandler(Environment.RegistrationsDatabase.Context);

        var result = await ErrorResult.CaptureAsync(async () =>
            await sut.HandleAsync(command, testContext.CancellationToken));

        result.Error.ShouldMatch(ConcurrencyConflictError.Create(fixture.SeededVersion + 99u, fixture.SeededVersion));
    }
}
