using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmails;

internal sealed record GetBulkEmailsQuery(
    TicketedEventId TicketedEventId,
    TeamId TeamId) : Query<IReadOnlyList<BulkEmailListItemDto>>;
