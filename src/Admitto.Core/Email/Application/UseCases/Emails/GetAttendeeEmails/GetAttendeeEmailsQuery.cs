using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails;

internal sealed record GetAttendeeEmailsQuery(
    TeamId TeamId,
    TicketedEventId EventId,
    RegistrationId RegistrationId) : Query<IReadOnlyList<AttendeeEmailLogItemDto>>;
