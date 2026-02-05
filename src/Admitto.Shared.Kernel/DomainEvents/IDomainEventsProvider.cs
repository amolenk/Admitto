namespace Amolenk.Admitto.Shared.Kernel.DomainEvents;

public interface IDomainEventsProvider
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();

    void ClearDomainEvents();
}