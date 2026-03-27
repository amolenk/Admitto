using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.ValueConverters;

internal sealed class TeamIdConverter() : ValueConverter<TeamId, Guid>(
    v => v.Value,
    v => TeamId.From(v));
