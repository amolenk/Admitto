using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;

internal sealed record GetAttendeeEmailsQuery(
    Guid TeamId,
    Guid EventId,
    Guid RegistrationId) : Query<IReadOnlyList<AttendeeEmailLogItemDto>>;
