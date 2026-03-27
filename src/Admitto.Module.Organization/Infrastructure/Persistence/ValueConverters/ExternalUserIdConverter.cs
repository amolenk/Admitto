using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.ValueConverters;

internal sealed class ExternalUserIdConverter() : ValueConverter<ExternalUserId, Guid>(
    v => v.Value,
    v => ExternalUserId.From(v));
