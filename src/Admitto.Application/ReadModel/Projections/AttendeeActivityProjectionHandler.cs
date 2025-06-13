using Amolenk.Admitto.Application.ReadModel.Views;
using Amolenk.Admitto.Domain.DomainEvents;

namespace Amolenk.Admitto.Application.ReadModel.Projections;

/// <summary>
/// Write to the activity log when a registration is confirmed.
/// </summary>
public class AttendeeActivityProjectionHandler(IReadModelContext context) 
    : IEventualDomainEventHandler<RegistrationAcceptedDomainEvent>
{
    public async ValueTask HandleAsync(RegistrationAcceptedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // TODO Guard against duplicate activities
        await context.AttendeeActivities.AddAsync(new AttendeeActivityView(domainEvent.Id, domainEvent.AttendeeId,
            "Registration accepted", domainEvent.OccurredOn), cancellationToken);
    }
}