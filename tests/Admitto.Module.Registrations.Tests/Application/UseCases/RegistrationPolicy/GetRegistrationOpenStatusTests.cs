using Amolenk.Admitto.Module.Email.Contracts;
using Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using NSubstitute;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.RegistrationPolicy;

[TestClass]
public sealed class GetRegistrationOpenStatusTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC013: Status reports canOpen=true when email is configured and lifecycle is Active.
    [TestMethod]
    public async ValueTask SC013_GetStatus_EmailConfiguredAndActive_CanOpenTrue()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.EventRegistrationPolicies.Add(EventRegistrationPolicy.Create(eventId));
        });

        var emailFacade = Substitute.For<IEventEmailFacade>();
        emailFacade.IsEmailConfiguredAsync(eventId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new GetRegistrationOpenStatusHandler(Environment.Database.Context, emailFacade);

        var result = await sut.HandleAsync(
            new GetRegistrationOpenStatusQuery(eventId), testContext.CancellationToken);

        result.Status.ShouldBe(RegistrationStatus.Draft);
        result.CanOpen.ShouldBeTrue();
        result.Reason.ShouldBeNull();
    }

    // SC014: Status reports canOpen=false with reason "email-not-configured".
    [TestMethod]
    public async ValueTask SC014_GetStatus_EmailNotConfigured_CanOpenFalse()
    {
        var eventId = TicketedEventId.New();

        await Environment.Database.SeedAsync(dbContext =>
        {
            dbContext.EventRegistrationPolicies.Add(EventRegistrationPolicy.Create(eventId));
        });

        var emailFacade = Substitute.For<IEventEmailFacade>();
        emailFacade.IsEmailConfiguredAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        var sut = new GetRegistrationOpenStatusHandler(Environment.Database.Context, emailFacade);

        var result = await sut.HandleAsync(
            new GetRegistrationOpenStatusQuery(eventId), testContext.CancellationToken);

        result.Status.ShouldBe(RegistrationStatus.Draft);
        result.CanOpen.ShouldBeFalse();
        result.Reason.ShouldBe("email-not-configured");
    }
}
