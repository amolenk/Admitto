using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail;

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
    : ICommandHandler<CreateBulkEmailCommand, Guid>
{
    public ValueTask<Guid> HandleAsync(CreateBulkEmailCommand command, CancellationToken cancellationToken)
    {
        TeamId teamId = TeamId.From(command.TeamId);
        TicketedEventId ticketedEventId = TicketedEventId.From(command.TicketedEventId);
        var triggeredBy = EmailAddress.From(userContext.Current.EmailAddress);

        var job = BulkEmailJob.Create(
            teamId,
            ticketedEventId,
            command.EmailType,
            command.TemplateName,
            command.Subject,
            command.TextBody,
            command.HtmlBody,
            command.Source,
            triggeredBy,
            timeProvider.GetUtcNow());

        writeStore.BulkEmailJobs.Add(job);
        return ValueTask.FromResult(job.Id.Value);
    }
}
