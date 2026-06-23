namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

internal sealed record OutboxDbContextRegistration(string ModuleKey, Type DbContextType);
