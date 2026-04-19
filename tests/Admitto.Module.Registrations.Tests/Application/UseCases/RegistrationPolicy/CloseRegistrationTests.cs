using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.CloseRegistration;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.RegistrationPolicy;

[TestClass]
public sealed class CloseRegistrationTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC011: Close an open event — status transitions Open → Closed.
    [TestMethod]
    public async ValueTask SC011_CloseRegistration_FromOpen_TransitionsToClosed()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.OpenForRegistration();
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var sut = new CloseRegistrationHandler(Environment.Database.Context);

        await sut.HandleAsync(new CloseRegistrationCommand(eventId), testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.RegistrationStatus.ShouldBe(RegistrationStatus.Closed);
        });
    }

    // SC012: Close is idempotent — Closed → Closed succeeds.
    [TestMethod]
    public async ValueTask SC012_CloseRegistration_AlreadyClosed_Idempotent()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.CloseForRegistration();
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var sut = new CloseRegistrationHandler(Environment.Database.Context);

        await sut.HandleAsync(new CloseRegistrationCommand(eventId), testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.RegistrationStatus.ShouldBe(RegistrationStatus.Closed);
        });
    }
}
