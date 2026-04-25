using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmails;

internal sealed record GetBulkEmailsQuery(
    TicketedEventId TicketedEventId) : Query<IReadOnlyList<BulkEmailListItemDto>>;
