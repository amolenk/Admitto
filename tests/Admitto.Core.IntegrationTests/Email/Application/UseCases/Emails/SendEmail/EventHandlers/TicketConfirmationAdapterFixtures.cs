using Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTicketConfirmation;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using NSubstitute;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.UseCases.Emails.SendEmail.EventHandlers;

internal sealed class AttendeeTicketsChangedIntegrationEventHandlerFixture
{
    private AttendeeTicketsChangedIntegrationEventHandlerFixture()
    {
        Composer = Substitute.For<ITicketConfirmationEmailComposer>();
        ChangedAt = new DateTimeOffset(2026, 8, 31, 12, 30, 0, TimeSpan.Zero);
        IntegrationEvent = new AttendeeTicketsChangedIntegrationEvent(
            TeamGuid,
            EventGuid,
            RegistrationGuid,
            "alice@example.com",
            "Alice",
            "Anderson",
            [new TicketTypeItem(Guid.NewGuid(), "General Admission")],
            ChangedAt);
    }

    public static AttendeeTicketsChangedIntegrationEventHandlerFixture Create() => new();

    public ITicketConfirmationEmailComposer Composer { get; }
    public DateTimeOffset ChangedAt { get; }
    public AttendeeTicketsChangedIntegrationEvent IntegrationEvent { get; }

    public static Guid TeamGuid { get; } = Guid.NewGuid();
    public static Guid EventGuid { get; } = Guid.NewGuid();
    public static Guid RegistrationGuid { get; } = Guid.NewGuid();
}

internal sealed class TicketConfirmationResendRequestedIntegrationEventHandlerFixture
{
    private TicketConfirmationResendRequestedIntegrationEventHandlerFixture()
    {
        Composer = Substitute.For<ITicketConfirmationEmailComposer>();
        ResendRequestId = Guid.NewGuid();
        IntegrationEvent = new TicketConfirmationResendRequestedIntegrationEvent(
            TeamGuid,
            EventGuid,
            RegistrationGuid,
            ResendRequestId,
            "alice@example.com",
            "Alice",
            "Anderson",
            ["General Admission"]);
    }

    public static TicketConfirmationResendRequestedIntegrationEventHandlerFixture Create() => new();

    public ITicketConfirmationEmailComposer Composer { get; }
    public Guid ResendRequestId { get; }
    public TicketConfirmationResendRequestedIntegrationEvent IntegrationEvent { get; }

    public static Guid TeamGuid { get; } = Guid.NewGuid();
    public static Guid EventGuid { get; } = Guid.NewGuid();
    public static Guid RegistrationGuid { get; } = Guid.NewGuid();
}
