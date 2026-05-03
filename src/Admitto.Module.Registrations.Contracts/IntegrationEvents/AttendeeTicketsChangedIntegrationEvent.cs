using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when an admin changes the ticket-type selection
/// on an existing registration. The Email module consumes this to send a confirmation email.
/// </summary>
public sealed record AttendeeTicketsChangedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    Guid RegistrationId,
    string RecipientEmail,
    string FirstName,
    string LastName,
    IReadOnlyList<TicketTypeItem> NewTickets,
    DateTimeOffset ChangedAt) : IntegrationEvent;

public sealed record TicketTypeItem(string Slug, string Name);
