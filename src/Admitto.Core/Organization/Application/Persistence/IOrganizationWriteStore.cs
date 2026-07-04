using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;

namespace Amolenk.Admitto.Core.Organization.Application.Persistence;

public interface IOrganizationWriteStore
{
    DbSet<Team> Teams { get; }

    DbSet<User> Users { get; }

    DbSet<ApiKey> ApiKeys { get; }

    DbSet<ProcessedMessage> ProcessedMessages { get; }
}
