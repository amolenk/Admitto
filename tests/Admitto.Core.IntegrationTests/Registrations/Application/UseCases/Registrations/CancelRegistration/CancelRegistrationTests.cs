using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

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
}
