using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Organization.Infrastructure.Persistence.ValueConverters;

internal sealed class ExternalUserIdConverter() : ValueConverter<ExternalUserId, Guid>(
    v => v.Value,
    v => ExternalUserId.From(v));
