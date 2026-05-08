using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CancelBulkEmail;

internal sealed record CancelBulkEmailCommand(Guid BulkEmailJobId) : Command;
