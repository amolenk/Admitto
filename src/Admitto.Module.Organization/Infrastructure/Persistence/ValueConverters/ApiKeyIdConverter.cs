using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.ValueConverters;

internal sealed class ApiKeyIdConverter() : ValueConverter<ApiKeyId, Guid>(
    v => v.Value,
    v => ApiKeyId.From(v));
