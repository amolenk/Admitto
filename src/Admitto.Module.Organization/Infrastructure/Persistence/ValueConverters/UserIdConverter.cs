using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.ValueConverters;

internal sealed class UserIdConverter() : ValueConverter<UserId, Guid>(
    v => v.Value,
    v => UserId.From(v));
