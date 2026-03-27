namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;

public interface IModuleDbContext
{
    static abstract string SchemaName { get; }
}
