using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.BulkEmails.GetBulkEmail;

internal sealed record GetBulkEmailQuery(BulkEmailJobId BulkEmailJobId) : Query<BulkEmailJobDetailDto?>;
