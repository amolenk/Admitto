using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail;

internal sealed record CreateBulkEmailCommand(
    TeamId TeamId,
    TicketedEventId TicketedEventId,
    string EmailType,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    BulkEmailJobSource Source) : Command<BulkEmailJobId>;
