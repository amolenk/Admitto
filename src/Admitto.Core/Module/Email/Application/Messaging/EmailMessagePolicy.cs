using Amolenk.Admitto.Core.Module.Email.Application.ModuleEvents;
using Amolenk.Admitto.Core.Module.Email.Domain.DomainEvents;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Contracts;
using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Module.Email.Application.Messaging;

/// <summary>
/// Email module's message policy. The bulk-email "requested" domain event is
/// republished as an internal module event so the worker host's command
/// handler can schedule the Quartz fan-out.
/// </summary>
public sealed class EmailMessagePolicy : MessagePolicy
{
    public EmailMessagePolicy()
    {
        Configure<BulkEmailJobRequestedDomainEvent>()
            .PublishModuleEvent(e => new BulkEmailJobRequestedModuleEvent
            {
                BulkEmailJobId = e.BulkEmailJobId.Value,
                TeamId = e.TeamId.Value,
                TicketedEventId = e.TicketedEventId.Value
            });
    }
}
