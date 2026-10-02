using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when the confirmed or waitlisted ticket-type selection
/// of an existing registration changes. The Email module consumes this to send a confirmation email.
/// </summary>
public sealed record AttendeeTicketsChangedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string RecipientEmail,
    string FirstName,
    string LastName,
    IReadOnlyList<TicketTypeItem> NewTickets,
    IReadOnlyList<TicketTypeItem> NewWaitlistedTickets,
    DateTimeOffset ChangedAt) : IntegrationEvent;

public sealed record TicketTypeItem(Guid Id, string Name);
