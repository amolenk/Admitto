using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when an attendee successfully registers.
/// The Email module consumes this to send a registration confirmation email.
/// <see cref="Tickets"/> are the confirmed ticket types; <see cref="WaitlistedTickets"/> are the ticket types
/// the attendee joined the waitlist for. A registration may hold either, or both.
/// </summary>
public sealed record AttendeeRegisteredIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string RecipientEmail,
    string FirstName,
    string LastName,
    IReadOnlyList<TicketTypeItem> Tickets,
    IReadOnlyList<TicketTypeItem> WaitlistedTickets,
    DateTimeOffset RegisteredAt) : IntegrationEvent;
