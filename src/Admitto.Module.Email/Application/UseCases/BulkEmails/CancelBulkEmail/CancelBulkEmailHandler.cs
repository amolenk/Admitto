using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CancelBulkEmail;

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
        var job = await writeStore.BulkEmailJobs
            .FirstOrDefaultAsync(j => j.Id == command.BulkEmailJobId, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<BulkEmailJob>(command.BulkEmailJobId.Value.ToString()));

        job.RequestCancellation(timeProvider.GetUtcNow());
    }
}
