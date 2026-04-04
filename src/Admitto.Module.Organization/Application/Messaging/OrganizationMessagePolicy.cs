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

        Configure<TicketTypeAddedDomainEvent>()
            .PublishModuleEvent(e => new TicketTypeAddedModuleEvent
            {
                TicketedEventId = e.TicketedEventId.Value,
                Slug = e.Slug,
                Name = e.Name,
                TimeSlots = e.TimeSlots,
                Capacity = e.Capacity
            });

        Configure<TicketTypeCapacityChangedDomainEvent>()
            .PublishModuleEvent(e => new TicketTypeCapacityChangedModuleEvent
            {
                TicketedEventId = e.TicketedEventId.Value,
                Slug = e.Slug,
                Capacity = e.Capacity
            });
    }
}