// namespace Amolenk.Admitto.Application.Features.Attendees.RegisterAttendee;
//
// public class SendRejectionEmailHandler(IEmailService emailService) : IRequestHandler<SendRejectionEmailCommand>
// {
//     public Task Handle(SendRejectionEmailCommand request, CancellationToken cancellationToken)
//     {
//         Console.WriteLine("Sending rejection email...");
//         return Task.CompletedTask;
// //        return emailService.SendRejectionEmailAsync(request.AttendeeId, request.TicketedEventId);
//     }
// }