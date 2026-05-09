using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;

internal sealed record GetAttendeeEmailsQuery(
    Guid TeamId,
    Guid EventId,
    Guid RegistrationId) : Query<IReadOnlyList<AttendeeEmailLogItemDto>>;
