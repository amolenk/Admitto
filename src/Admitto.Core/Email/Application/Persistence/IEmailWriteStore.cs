using Amolenk.Admitto.Core.Email.Domain.Entities;

namespace Amolenk.Admitto.Core.Email.Application.Persistence;

public interface IEmailWriteStore
{
    DbSet<EmailSettings> EmailSettings { get; }
    DbSet<EmailLog> EmailLog { get; }
    DbSet<BulkEmailJob> BulkEmailJobs { get; }
}
