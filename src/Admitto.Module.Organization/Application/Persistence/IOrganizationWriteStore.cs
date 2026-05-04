using Amolenk.Admitto.Module.Organization.Domain.Entities;

namespace Amolenk.Admitto.Module.Organization.Application.Persistence;

public interface IOrganizationWriteStore
{
    DbSet<Team> Teams { get; }

    DbSet<User> Users { get; }

    DbSet<ApiKey> ApiKeys { get; }
}
