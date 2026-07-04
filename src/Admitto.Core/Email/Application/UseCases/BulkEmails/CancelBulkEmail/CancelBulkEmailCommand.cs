using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CancelBulkEmail;

internal sealed record CancelBulkEmailCommand(Guid BulkEmailJobId, Guid TicketedEventId, Guid TeamId) : Command;
