using Amolenk.Admitto.Module.Registrations.Application.ModuleEvents;
using Amolenk.Admitto.Module.Registrations.Domain.DomainEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.Messaging;

public class RegistrationsMessagePolicy : MessagePolicy
{
    public RegistrationsMessagePolicy()
    {
        Configure<CouponCreatedDomainEvent>()
            .PublishModuleEvent(e => new CouponCreatedModuleEvent
            {
                CouponId = e.CouponId.Value,
                TicketedEventId = e.TicketedEventId.Value,
                Email = e.Email.Value
            });
    }
}
