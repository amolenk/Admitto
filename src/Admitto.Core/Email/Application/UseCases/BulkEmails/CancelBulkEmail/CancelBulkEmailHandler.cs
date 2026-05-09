using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CancelBulkEmail;

/// <summary>
/// Records a cooperative cancellation request on a <see cref="BulkEmailJob"/>.
/// The Worker observes <c>CancellationRequestedAt</c> between recipients and
/// finalises the job to <c>Cancelled</c>; this handler does not block.
/// </summary>
internal sealed class CancelBulkEmailHandler(
    IEmailWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<CancelBulkEmailCommand>
{
    public async ValueTask HandleAsync(CancelBulkEmailCommand command, CancellationToken cancellationToken)
    {
        BulkEmailJobId bulkEmailJobId = BulkEmailJobId.From(command.BulkEmailJobId);

        var job = await writeStore.BulkEmailJobs
            .FirstOrDefaultAsync(j => j.Id == bulkEmailJobId, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<BulkEmailJob>(bulkEmailJobId.Value.ToString()));

        job.RequestCancellation(timeProvider.GetUtcNow());
    }
}
