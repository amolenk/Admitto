using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails;

internal sealed record GetBulkEmailsQuery(
    TicketedEventId TicketedEventId) : Query<IReadOnlyList<BulkEmailListItemDto>>;
