using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery;
using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.Composing;
using Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.PrepareEmailDelivery.EventHandlers;
using Amolenk.Admitto.Core.Registrations.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.CancelRegistration;

[TestClass]
public sealed class CancelRegistrationTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active registration
    // When it is cancelled with reason AttendeeRequest
    // Then the registration's cancellation reason is set to AttendeeRequest
    [TestMethod]
    public async ValueTask CancelRegistration_AttendeeRequest_SetsCancelledState()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);
        });
    }

    // Given an active registration
    // When it is cancelled with reason VisaLetterDenied
    // Then the registration's cancellation reason is set to VisaLetterDenied
    [TestMethod]
    public async ValueTask CancelRegistration_VisaLetterDenied_SetsCancelledState()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value,
            CancellationReason.VisaLetterDenied);
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.CancellationReason.ShouldBe(CancellationReason.VisaLetterDenied);
        });
    }

    // Given a registration that is already cancelled
    // When it is cancelled again
    // Then it fails with an already-cancelled error
    [TestMethod]
    public async ValueTask CancelRegistration_AlreadyCancelled_ThrowsAlreadyCancelledError()
    {
        var fixture = CancelRegistrationFixture.AlreadyCancelled();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Registration.Errors.AlreadyCancelled);
    }

    // Given no registration exists with the given id
    // When a cancellation is requested for that id
    // Then it fails with a not-found error
    [TestMethod]
    public async ValueTask CancelRegistration_RegistrationNotFound_ThrowsNotFoundError()
    {
        var unknownId = RegistrationId.New();
        var command = new CancelRegistrationCommand(
            unknownId.Value,
            TicketedEventId.New().Value,
            TeamId.New().Value,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(NotFoundError.Create<Registration>());
    }

    // Given an active registration that belongs to a different event and team
    // When a cancellation is requested using the wrong event and team id
    // Then it fails with a not-found error
    [TestMethod]
    public async ValueTask CancelRegistration_WrongEventId_ThrowsNotFoundError()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId.Value,
            TicketedEventId.New().Value,   // wrong event
            TeamId.New().Value,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(NotFoundError.Create<Registration>());
    }

    // Given a registration for an event that has already started
    // When the attendee requests self-service cancellation
    // Then it fails with an event-already-started error
    [TestMethod]
    public async ValueTask CancelRegistration_AttendeeRequest_EventAlreadyStarted_ThrowsConflict()
    {
        var fixture = CancelRegistrationFixture.WithEventAlreadyStarted();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(CancelRegistrationHandler.Errors.EventAlreadyStarted);
    }

    // Given a registration for an event that has not yet started
    // When the attendee requests self-service cancellation
    // Then the registration's cancellation reason is set to AttendeeRequest
    [TestMethod]
    public async ValueTask CancelRegistration_AttendeeRequest_EventNotYetStarted_SetsCancelledState()
    {
        var fixture = CancelRegistrationFixture.WithEventNotYetStarted();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);
        });
    }

    // Given a registration that holds no confirmed tickets and is only on a waitlist
    // When the attendee cancels it
    // Then the attendee is sent the waitlist-removal cancellation email rather than the ticket cancellation email
    [TestMethod]
    public async ValueTask CancelRegistration_WaitlistedRegistration_SendsWaitlistCancellationEmail()
    {
        var fixture = CancelRegistrationFixture.WaitlistedRegistration();
        await fixture.SetupAsync(Environment);

        var delivery = await CancelAndPrepareEmailAsync(fixture);

        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.WaitlistCancellation);
        delivery.Subject.ShouldBe("You've been removed from the DevConf waitlist");
        delivery.TextBody.ShouldContain("removed you from the waitlist for DevConf");
        delivery.TextBody.ShouldNotContain("cancel your registration");
    }

    // Given a registration that holds a confirmed ticket and is also on another ticket type's waitlist
    // When the attendee cancels it
    // Then the attendee is sent the existing ticket cancellation email
    [TestMethod]
    public async ValueTask CancelRegistration_RegisteredWithWaitlistEntry_SendsExistingCancellationEmail()
    {
        var fixture = CancelRegistrationFixture.RegisteredWithWaitlistEntry();
        await fixture.SetupAsync(Environment);

        var delivery = await CancelAndPrepareEmailAsync(fixture);

        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.Cancellation);
        delivery.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");
        delivery.TextBody.ShouldContain("We’ve processed your request to cancel your registration for DevConf.");
    }

    // Given a registration that holds a confirmed ticket and no waitlist entries
    // When the attendee cancels it
    // Then the attendee is sent the existing ticket cancellation email
    [TestMethod]
    public async ValueTask CancelRegistration_RegisteredRegistration_SendsExistingCancellationEmail()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var delivery = await CancelAndPrepareEmailAsync(fixture);

        delivery.EmailType.ShouldBe(BuiltInEmailTemplateNames.Cancellation);
        delivery.Subject.ShouldBe("Your DevConf Registration Has Been Cancelled");
        delivery.TextBody.ShouldContain("We’ve processed your request to cancel your registration for DevConf.");
    }

    /// <summary>
    /// Cancels the fixture's registration at the attendee's request, then drives the raised domain
    /// event through the real integration event publisher, cancellation email adapter, and composer,
    /// returning the single prepared email delivery.
    /// </summary>
    private async ValueTask<PrepareEmailDeliveryCommand> CancelAndPrepareEmailAsync(CancelRegistrationFixture fixture)
    {
        var sut = new CancelRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);
        await sut.HandleAsync(
            new CancelRegistrationCommand(
                fixture.RegistrationId.Value,
                fixture.EventId.Value,
                fixture.TeamId.Value,
                CancellationReason.AttendeeRequest),
            testContext.CancellationToken);

        var registration = await Environment.RegistrationsDatabase.Context.Registrations
            .FirstAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
        var domainEvent = registration.GetDomainEvents()
            .OfType<RegistrationCancelledDomainEvent>()
            .ShouldHaveSingleItem();

        var outbox = Substitute.For<IOutbox>();
        IIntegrationEvent? capturedIntegrationEvent = null;
        outbox.When(o => o.Enqueue(Arg.Any<IIntegrationEvent>()))
            .Do(ci => capturedIntegrationEvent = ci.Arg<IIntegrationEvent>());
        await new RegistrationsIntegrationEventPublisher(outbox)
            .HandleAsync(domainEvent, testContext.CancellationToken);
        var integrationEvent = capturedIntegrationEvent.ShouldBeOfType<RegistrationCancelledIntegrationEvent>();

        var composerFixture = TransactionalEmailComposerFixture.CompleteEventContext();
        await composerFixture.SetupAsync(Environment, fixture.TeamId, fixture.EventId);
        var deliveryHandler = Substitute.For<ICommandHandler<PrepareEmailDeliveryCommand>>();
        await new RegistrationCancelledIntegrationEventHandler(composerFixture.BuildComposer(Environment), deliveryHandler)
            .HandleAsync(integrationEvent, testContext.CancellationToken);

        return deliveryHandler.ReceivedDelivery();
    }
}
