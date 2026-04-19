using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.ValueConverters;

internal sealed class PortConverter() : ValueConverter<Port, int>(
    v => v.Value,
    v => Port.From(v));
