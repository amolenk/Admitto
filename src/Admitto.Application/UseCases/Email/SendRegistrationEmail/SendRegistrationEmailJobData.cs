// using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.UseCases.Email.SendRegistrationEmail;
//
// public record SendRegistrationEmailJobData(
//     Guid JobId,
//     Guid TeamId,
//     Guid TicketedEventId,
//     EmailType Type,
//     Guid PendingRegistrationId,
//     string? RecipientEmail = null)
//     : SendEmailJobData(JobId, TeamId, TicketedEventId, Type, RecipientEmail);