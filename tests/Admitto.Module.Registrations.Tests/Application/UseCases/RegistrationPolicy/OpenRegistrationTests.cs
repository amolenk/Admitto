using Amolenk.Admitto.Module.Email.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.OpenRegistration;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.RegistrationPolicy;

[TestClass]
public sealed class OpenRegistrationTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC007: Open event when email is configured — Draft policy transitions to Open.
    [TestMethod]
    public async ValueTask SC007_OpenRegistration_EmailConfigured_TransitionsToOpen()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.EventRegistrationPolicies.Add(EventRegistrationPolicy.Create(eventId));
        });

        var emailFacade = Substitute.For<IEventEmailFacade>();
        emailFacade.IsEmailConfiguredAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new OpenRegistrationHandler(Environment.Database.Context, emailFacade);

        await sut.HandleAsync(new OpenRegistrationCommand(eventId), testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.RegistrationStatus.ShouldBe(RegistrationStatus.Open);
        });
    }

    // SC008: Open rejected when email is not configured — status remains unchanged.
    [TestMethod]
    public async ValueTask SC008_OpenRegistration_EmailNotConfigured_Rejected()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.EventRegistrationPolicies.Add(EventRegistrationPolicy.Create(eventId));
        });

        var emailFacade = Substitute.For<IEventEmailFacade>();
        emailFacade.IsEmailConfiguredAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        var sut = new OpenRegistrationHandler(Environment.Database.Context, emailFacade);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(new OpenRegistrationCommand(eventId), testContext.CancellationToken); });

        result.Error.ShouldMatch(OpenRegistrationHandler.Errors.EmailNotConfigured);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.RegistrationStatus.ShouldBe(RegistrationStatus.Draft);
        });
    }

    // SC009: Open rejected when lifecycle is Cancelled.
    [TestMethod]
    public async ValueTask SC009_OpenRegistration_LifecycleCancelled_Rejected()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.SetCancelled();
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var emailFacade = Substitute.For<IEventEmailFacade>();
        emailFacade.IsEmailConfiguredAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new OpenRegistrationHandler(Environment.Database.Context, emailFacade);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(new OpenRegistrationCommand(eventId), testContext.CancellationToken); });

        result.Error.ShouldMatch(EventRegistrationPolicy.Errors.EventNotActive);
    }

    // SC010: Re-open a previously closed event.
    [TestMethod]
    public async ValueTask SC010_OpenRegistration_FromClosed_TransitionsToOpen()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            var policy = EventRegistrationPolicy.Create(eventId);
            policy.CloseForRegistration();
            dbContext.EventRegistrationPolicies.Add(policy);
        });

        var emailFacade = Substitute.For<IEventEmailFacade>();
        emailFacade.IsEmailConfiguredAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new OpenRegistrationHandler(Environment.Database.Context, emailFacade);

        await sut.HandleAsync(new OpenRegistrationCommand(eventId), testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var policy = await dbContext.EventRegistrationPolicies
                .SingleOrDefaultAsync(testContext.CancellationToken);
            policy.ShouldNotBeNull();
            policy.RegistrationStatus.ShouldBe(RegistrationStatus.Open);
        });
    }
}
