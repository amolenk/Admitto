using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReconfirmRegistration;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.ReconfirmRegistration;

[TestClass]
public sealed class ReconfirmRegistrationTests(TestContext testContext) : AspireIntegrationTestBase
{
    // Given an active, unreconfirmed registration
    // When it is reconfirmed
    // Then the registration is marked as reconfirmed
    [TestMethod]
    public async ValueTask ReconfirmRegistration_ActiveRegistration_MarksAsReconfirmed()
    {
        var fixture = ReconfirmRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var command = new ReconfirmRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value);
        var sut = new ReconfirmRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.RegistrationsDatabase.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.HasReconfirmed.ShouldBeTrue();
        });
    }

    // Given a registration that is already cancelled
    // When reconfirmation is requested
    // Then it fails with an already-cancelled error
    [TestMethod]
    public async ValueTask ReconfirmRegistration_AlreadyCancelled_ThrowsAlreadyCancelledError()
    {
        var fixture = ReconfirmRegistrationFixture.AlreadyCancelled();
        await fixture.SetupAsync(Environment);

        var command = new ReconfirmRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value);
        var sut = new ReconfirmRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Registration.Errors.CannotReconfirmCancelled);
    }

    // Given a registration for an event whose catalog has been archived
    // When an admin requests reconfirmation
    // Then it fails with an event-not-active error
    [TestMethod]
    public async ValueTask ReconfirmRegistration_EventArchived_ThrowsEventNotActiveError()
    {
        var fixture = ReconfirmRegistrationFixture.WithArchivedEvent();
        await fixture.SetupAsync(Environment);

        var command = new ReconfirmRegistrationCommand(
            fixture.RegistrationId.Value,
            fixture.EventId.Value,
            fixture.TeamId.Value);
        var sut = new ReconfirmRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(TicketCatalog.Errors.EventNotActive);
    }

    // Given no registration exists with the given id
    // When reconfirmation is requested for that id
    // Then it fails with a not-found error
    [TestMethod]
    public async ValueTask ReconfirmRegistration_RegistrationNotFound_ThrowsNotFoundError()
    {
        var unknownId = RegistrationId.New();
        var command = new ReconfirmRegistrationCommand(
            unknownId.Value,
            TicketedEventId.New().Value,
            TeamId.New().Value);
        var sut = new ReconfirmRegistrationHandler(Environment.RegistrationsDatabase.Context, TimeProvider.System);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(NotFoundError.Create<Registration>());
    }
}
