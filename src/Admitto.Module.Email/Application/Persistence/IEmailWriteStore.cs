using Amolenk.Admitto.Module.Email.Domain.Entities;

namespace Amolenk.Admitto.Module.Email.Application.Persistence;

public interface IEmailWriteStore
{
    DbSet<EventEmailSettings> EventEmailSettings { get; }
}
