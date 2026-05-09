namespace Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

public interface IDomainEventsProvider
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();

    void ClearDomainEvents();
}