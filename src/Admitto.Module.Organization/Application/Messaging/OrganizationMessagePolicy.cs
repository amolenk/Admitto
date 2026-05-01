using Amolenk.Admitto.Module.Organization.Application.ModuleEvents;
using Amolenk.Admitto.Module.Organization.Contracts;
using Amolenk.Admitto.Module.Organization.Contracts.IntegrationEvents;
using Amolenk.Admitto.Module.Organization.Domain.DomainEvents;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.Messaging;

public class OrganizationMessagePolicy : MessagePolicy
{
    public OrganizationMessagePolicy()
    {
        Configure<UserCreatedDomainEvent>()
            .PublishModuleEvent(e => new UserCreatedModuleEvent(e.UserId.Value));

        Configure<TicketedEventCreationRequestedDomainEvent>()
            .PublishIntegrationEvent(e => new TicketedEventCreationRequested(
                e.CreationRequestId.Value,
                e.TeamId.Value,
                e.TeamSlug.Value,
                e.Slug.Value,
                e.Name.Value,
                e.WebsiteUrl.Value.ToString(),
                e.BaseUrl.Value.ToString(),
                e.StartsAt,
                e.EndsAt,
                e.TimeZone.Value));
    }
}
