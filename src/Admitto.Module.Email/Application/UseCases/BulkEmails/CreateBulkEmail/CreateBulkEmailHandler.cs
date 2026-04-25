using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.CreateBulkEmail;

/// <summary>
/// Creates a user-triggered <see cref="BulkEmailJob"/>. The aggregate raises
/// <c>BulkEmailJobRequestedDomainEvent</c>, which is mapped to
/// <c>BulkEmailJobRequestedModuleEvent</c> by <see cref="Messaging.EmailMessagePolicy"/>
/// and ultimately schedules a Quartz one-shot trigger in the Worker host.
/// </summary>
internal sealed class CreateBulkEmailHandler(
    IEmailWriteStore writeStore,
    IUserContextAccessor userContext,
    TimeProvider timeProvider)
    : ICommandHandler<CreateBulkEmailCommand, BulkEmailJobId>
{
    public ValueTask<BulkEmailJobId> HandleAsync(CreateBulkEmailCommand command, CancellationToken cancellationToken)
    {
        var triggeredBy = EmailAddress.From(userContext.Current.EmailAddress);

        var job = BulkEmailJob.Create(
            command.TeamId,
            command.TicketedEventId,
            command.EmailType,
            command.Subject,
            command.TextBody,
            command.HtmlBody,
            command.Source,
            triggeredBy,
            timeProvider.GetUtcNow());

        writeStore.BulkEmailJobs.Add(job);
        return ValueTask.FromResult(job.Id);
    }
}
