using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Registrations.Application.Mapping;
using Amolenk.Admitto.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Registrations.Application.Persistence;

public static class RegistrationPersistenceExtensions
{
    extension(DbSet<RegistrationRecord> dbSet)
    {
        public async ValueTask<Registration> LoadAggregateAsync(
            RegistrationId id,
            CancellationToken cancellationToken = default)
        {
            var record = await dbSet.FindAsync([id], cancellationToken);

            return record is not null
                ? record.ToDomain()
                : throw new BusinessRuleViolationException(Errors.RegistrationNotFound(id));
        }

        public void SaveAggregate(Registration registration)
        {
            dbSet.ApplyDomainChanges(
                registration,
                r => r.Id.Value,
                (domain, record) => record.ApplyFromDomain(domain),
                RegistrationRecord.FromDomain);
        }
    }

    private static class Errors
    {
        public static Error RegistrationNotFound(RegistrationId id) =>
            new(
                "registration_not_found",
                "Registration could not be found.",
                Details: new Dictionary<string, object?> { ["registrationId"] = id });
    }
}