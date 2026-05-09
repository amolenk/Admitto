using Amolenk.Admitto.Core.Module.Organization.Domain.Entities;

namespace Amolenk.Admitto.Core.Module.Organization.Application.Persistence;

public interface IOrganizationWriteStore
{
    DbSet<Team> Teams { get; }

    DbSet<User> Users { get; }

    DbSet<ApiKey> ApiKeys { get; }
}
