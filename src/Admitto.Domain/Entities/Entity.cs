namespace Amolenk.Admitto.Domain.Entities;

public abstract class Entity
{
    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; protected set; } = Guid.NewGuid();
}