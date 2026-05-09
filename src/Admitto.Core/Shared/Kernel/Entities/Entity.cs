namespace Amolenk.Admitto.Core.Shared.Kernel.Entities;

public abstract class Entity<TId>
{
    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; protected set; } = default!;
}
