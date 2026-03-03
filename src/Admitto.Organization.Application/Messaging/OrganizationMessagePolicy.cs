using Amolenk.Admitto.Organization.Application.ModuleEvents;
using Amolenk.Admitto.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.Messaging;

public class OrganizationMessagePolicy : MessagePolicy
{
    public OrganizationMessagePolicy()
    {
        Configure<UserCreatedDomainEvent>()
            .PublishModuleEvent(e => new UserCreatedModuleEvent(e.UserId.Value));
    }
}