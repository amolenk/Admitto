using Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RequestTicketConfirmationResend;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Amolenk.Admitto.Testing.Infrastructure.Assertions;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.IntegrationTests.Registrations.Application.UseCases.Registrations.RequestTicketConfirmationResend;

[TestClass]
public sealed class RequestTicketConfirmationResendHandlerTests(TestContext testContext) : AspireIntegrationTestBase
{
    private static readonly TicketTypeId GeneralTicketId = TicketTypeId.New();
    private const string GeneralTicketName = "General Admission";

    [TestMethod]
    public async ValueTask HandleAsync_RegisteredRegistration_EnqueuesResendRequestedEvent()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredRegistration();
        await fixture.SetupAsync(Environment);

        await NewHandler().HandleAsync(fixture.Command(), testContext.CancellationToken);
        await Environment.RegistrationsDatabase.Context.SaveChangesAsync(testContext.CancellationToken);

        var message = await Environment.RegistrationsDatabase.Context.OutboxMessages
            .AsNoTracking()
            .SingleAsync(m => m.Type == "Registrations:TicketConfirmationResendRequestedIntegrationEvent", testContext.CancellationToken);

        var payload = message.Payload.RootElement;
        payload.GetProperty("teamId").GetGuid().ShouldBe(fixture.TeamId.Value);
        payload.GetProperty("ticketedEventId").GetGuid().ShouldBe(fixture.EventId.Value);
        payload.GetProperty("registrationId").GetGuid().ShouldBe(fixture.RegistrationId.Value);
        payload.GetProperty("recipientEmail").GetString().ShouldBe("alice@example.com");
        payload.GetProperty("firstName").GetString().ShouldBe("Alice");
        payload.GetProperty("lastName").GetString().ShouldBe("Doe");
        payload.GetProperty("ticketNames")[0].GetString().ShouldBe(GeneralTicketName);
    }

    [TestMethod]
    public async ValueTask HandleAsync_MissingRegistration_ThrowsNotFoundError()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredRegistration();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () =>
            await NewHandler().HandleAsync(
                fixture.Command(registrationId: RegistrationId.New()),
                testContext.CancellationToken));

        result.Error.ShouldMatch(NotFoundError.Create<Registration>());
    }

    [TestMethod]
    public async ValueTask HandleAsync_WrongTeamScope_ThrowsNotFoundError()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredRegistration();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () =>
            await NewHandler().HandleAsync(
                fixture.Command(teamId: TeamId.New()),
                testContext.CancellationToken));

        result.Error.ShouldMatch(NotFoundError.Create<Registration>());
    }

    [TestMethod]
    public async ValueTask HandleAsync_WrongEventScope_ThrowsNotFoundError()
    {
        var fixture = RequestTicketConfirmationResendFixture.RegisteredRegistration();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () =>
            await NewHandler().HandleAsync(
                fixture.Command(eventId: TicketedEventId.New()),
                testContext.CancellationToken));

        result.Error.ShouldMatch(NotFoundError.Create<Registration>());
    }

    [TestMethod]
    public async ValueTask HandleAsync_CancelledRegistration_ThrowsNotRegisteredError()
    {
        var fixture = RequestTicketConfirmationResendFixture.CancelledRegistration();
        await fixture.SetupAsync(Environment);

        var result = await ErrorResult.CaptureAsync(async () =>
            await NewHandler().HandleAsync(fixture.Command(), testContext.CancellationToken));

        result.Error.ShouldMatch(RequestTicketConfirmationResendHandler.Errors.RegistrationNotRegistered);
    }

    private static RequestTicketConfirmationResendHandler NewHandler() =>
        new(Environment.RegistrationsDatabase.Context, new Outbox(Environment.RegistrationsDatabase.Context));

    private sealed class RequestTicketConfirmationResendFixture
    {
        public TeamId TeamId { get; } = TeamId.New();
        public TicketedEventId EventId { get; } = TicketedEventId.New();
        public RegistrationId RegistrationId { get; private set; } = RegistrationId.New();

        private readonly bool _cancelled;

        private RequestTicketConfirmationResendFixture(bool cancelled) => _cancelled = cancelled;

        public static RequestTicketConfirmationResendFixture RegisteredRegistration() => new(cancelled: false);

        public static RequestTicketConfirmationResendFixture CancelledRegistration() => new(cancelled: true);

        public RequestTicketConfirmationResendCommand Command(
            RegistrationId? registrationId = null,
            TeamId? teamId = null,
            TicketedEventId? eventId = null) =>
            new(
                (teamId ?? TeamId).Value,
                (eventId ?? EventId).Value,
                (registrationId ?? RegistrationId).Value,
                Guid.NewGuid());

        public async ValueTask SetupAsync(IntegrationTestEnvironment environment)
        {
            var registration = Registration.Create(
                TeamId,
                EventId,
                EmailAddress.From("alice@example.com"),
                FirstName.From("Alice"),
                LastName.From("Doe"),
                [new TicketTypeSnapshot(GeneralTicketId, TicketTypeName.From(GeneralTicketName), [])]);

            if (_cancelled)
                registration.Cancel(CancellationReason.AttendeeRequest);

            RegistrationId = registration.Id;
            await environment.RegistrationsDatabase.SeedAsync(db => db.Registrations.Add(registration));
        }
    }
}
