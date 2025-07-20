// using Amolenk.Admitto.Application.UseCases.Email.SendEmail;
// using Amolenk.Admitto.Domain.Entities;
// using Amolenk.Admitto.Domain.ValueObjects;
//
// namespace Amolenk.Admitto.Application.UseCases.Email.SendAttendeeEmail;
//
// public record SendAttendeeEmailJobData(
//     Guid JobId,
//     Guid TeamId,
//     Guid TicketedEventId,
//     EmailType Type,
//     Guid AttendeeId)
//     : SendEmailJobData(JobId, TeamId, TicketedEventId, Type);