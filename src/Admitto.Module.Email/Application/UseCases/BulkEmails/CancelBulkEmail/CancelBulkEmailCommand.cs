using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CancelBulkEmail;

internal sealed record CancelBulkEmailCommand(BulkEmailJobId BulkEmailJobId) : Command;
