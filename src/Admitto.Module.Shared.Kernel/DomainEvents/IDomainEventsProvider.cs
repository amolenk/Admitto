namespace Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;

public interface IDomainEventsProvider
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();

    void ClearDomainEvents();
}