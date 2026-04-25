using Amolenk.Admitto.Module.Email.Domain.Entities;

namespace Amolenk.Admitto.Module.Email.Application.Persistence;

public interface IEmailWriteStore
{
    DbSet<EmailSettings> EmailSettings { get; }
    DbSet<EmailTemplate> EmailTemplates { get; }
    DbSet<EmailLog> EmailLog { get; }
    DbSet<BulkEmailJob> BulkEmailJobs { get; }
}
