using Amolenk.Admitto.Module.Organization.Application.ModuleEvents;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.Messaging;

public class OrganizationMessagePolicy : MessagePolicy
{
    public OrganizationMessagePolicy()
    {
        Configure<UserCreatedDomainEvent>()
            .PublishModuleEvent(e => new UserCreatedModuleEvent(e.UserId.Value));

        Configure<TicketedEventCancelledDomainEvent>()
            .PublishModuleEvent(e => new TicketedEventCancelledModuleEvent(e.TicketedEventId.Value));

        Configure<TicketedEventArchivedDomainEvent>()
            .PublishModuleEvent(e => new TicketedEventArchivedModuleEvent(e.TicketedEventId.Value));
    }
}