using Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.CancelRegistration;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Tests.Application.Aspire;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Tests.Application.UseCases.Registrations.CancelRegistration;

[TestClass]
public sealed class CancelRegistrationTests(TestContext testContext) : AspireIntegrationTestBase
{
    // SC-C01: Admin cancels active registration with AttendeeRequest — sets IsCancelled
    [TestMethod]
    public async ValueTask SC001_CancelRegistration_AttendeeRequest_SetsCancelledState()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId,
            fixture.EventId,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.CancellationReason.ShouldBe(CancellationReason.AttendeeRequest);
        });
    }

    // SC-C02: Admin cancels active registration with VisaLetterDenied — sets IsCancelled
    [TestMethod]
    public async ValueTask SC002_CancelRegistration_VisaLetterDenied_SetsCancelledState()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId,
            fixture.EventId,
            CancellationReason.VisaLetterDenied);
        var sut = new CancelRegistrationHandler(Environment.Database.Context);

        await sut.HandleAsync(command, testContext.CancellationToken);

        await Environment.Database.AssertAsync(async dbContext =>
        {
            var registration = await dbContext.Registrations
                .FirstOrDefaultAsync(r => r.Id == fixture.RegistrationId, testContext.CancellationToken);
            registration.ShouldNotBeNull();
            registration.CancellationReason.ShouldBe(CancellationReason.VisaLetterDenied);
        });
    }

    // SC-C03: Admin cancels already-cancelled registration — throws already_cancelled (409)
    [TestMethod]
    public async ValueTask SC003_CancelRegistration_AlreadyCancelled_ThrowsAlreadyCancelledError()
    {
        var fixture = CancelRegistrationFixture.AlreadyCancelled();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId,
            fixture.EventId,
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(Registration.Errors.AlreadyCancelled);
    }

    // SC-C04: Admin cancels non-existent registration — throws not_found (404)
    [TestMethod]
    public async ValueTask SC004_CancelRegistration_RegistrationNotFound_ThrowsNotFoundError()
    {
        var unknownId = RegistrationId.New();
        var command = new CancelRegistrationCommand(
            unknownId,
            TicketedEventId.New(),
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(NotFoundError.Create<Registration>(unknownId.Value));
    }

    // SC-C05: Admin cancels registration from wrong event — returns not_found (no info leak)
    [TestMethod]
    public async ValueTask SC005_CancelRegistration_WrongEventId_ThrowsNotFoundError()
    {
        var fixture = CancelRegistrationFixture.ActiveRegistration();
        await fixture.SetupAsync(Environment);

        var command = new CancelRegistrationCommand(
            fixture.RegistrationId,
            TicketedEventId.New(),   // wrong event
            CancellationReason.AttendeeRequest);
        var sut = new CancelRegistrationHandler(Environment.Database.Context);

        var result = await ErrorResult.CaptureAsync(
            async () => { await sut.HandleAsync(command, testContext.CancellationToken); });

        result.Error.ShouldMatch(NotFoundError.Create<Registration>(fixture.RegistrationId.Value));
    }
}
