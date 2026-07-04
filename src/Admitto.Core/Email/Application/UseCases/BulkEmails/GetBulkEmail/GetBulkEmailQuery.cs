using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.GetBulkEmail;

internal sealed record GetBulkEmailQuery(
    BulkEmailJobId BulkEmailJobId,
    TicketedEventId TicketedEventId,
    TeamId TeamId) : Query<BulkEmailJobDetailDto?>;
